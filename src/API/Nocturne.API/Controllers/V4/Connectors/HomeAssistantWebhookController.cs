using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nocturne.Connectors.Core.Models;
using Nocturne.Connectors.HomeAssistant.Configurations;
using Nocturne.Connectors.HomeAssistant.Mappers;
using Nocturne.Connectors.HomeAssistant.Models;
using Nocturne.Core.Contracts.Connectors;
using Nocturne.Core.Contracts.Glucose;

namespace Nocturne.API.Controllers.V4.Connectors;

/// <summary>
/// Receives inbound webhooks from Home Assistant automations.
/// Authentication is via secret in URL path, not standard auth middleware.
/// Configuration is loaded from the database at request time (not the startup singleton)
/// so that webhook settings can be changed without restarting the application.
/// </summary>
/// <seealso cref="IConnectorConfigurationService"/>
/// <seealso cref="IEntryService"/>
// TODO: In multitenant deployments, webhook URL should include tenant context.
// Current implementation works for single-tenant setups. For multitenant:
// either encode tenant ID in URL (/webhook/{tenantId}/{secret}) or
// have the webhook secret map to a specific tenant.
[ApiController]
[Route("api/v4/connectors/home-assistant/webhook")]
[AllowAnonymous]
public class HomeAssistantWebhookController(
    IConnectorConfigurationService configService,
    HomeAssistantEntityMapper mapper,
    IEntryService entryService,
    ILogger<HomeAssistantWebhookController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Receives a webhook from Home Assistant with entity state updates and creates
    /// corresponding glucose entries.
    /// </summary>
    /// <param name="secret">Shared webhook secret used to authenticate the request.</param>
    /// <param name="payload">Home Assistant state response containing entity data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success; 401 if the secret is invalid; 404 if not configured.</returns>
    [HttpPost("{secret}")]
    public async Task<IActionResult> ReceiveWebhook(
        string secret,
        [FromBody] HomeAssistantStateResponse payload,
        CancellationToken ct)
    {
        // Load runtime configuration from DB (same source as the background service)
        var dbConfig = await configService.GetConfigurationAsync("HomeAssistant", ct);
        if (dbConfig?.Configuration == null)
            return NotFound();

        var config = JsonSerializer.Deserialize<HomeAssistantConnectorConfiguration>(
            dbConfig.Configuration.RootElement.GetRawText(), JsonOptions)
            ?? new();

        // Apply decrypted secrets (webhookSecret is stored encrypted)
        var secrets = await configService.GetSecretsAsync("HomeAssistant", ct);
        if (secrets.TryGetValue("webhookSecret", out var webhookSecret))
            config.WebhookSecret = webhookSecret;

        if (!config.WebhookEnabled || string.IsNullOrEmpty(config.WebhookSecret))
            return NotFound();

        if (!string.Equals(secret, config.WebhookSecret, StringComparison.Ordinal))
            return Unauthorized();

        // Check if this entity is mapped
        var mapping = config.EntityMappings
            .FirstOrDefault(m => m.Value == payload.EntityId);

        if (mapping.Value == null)
        {
            logger.LogWarning("Received webhook for unmapped entity {EntityId}", payload.EntityId);
            return BadRequest("Entity not mapped");
        }

        var dataType = mapping.Key;

        if (dataType != SyncDataType.Glucose)
        {
            return BadRequest(
                $"Webhook only supports Glucose data type. " +
                $"Use polling for {dataType}.");
        }

        var entry = mapper.MapToEntry(payload);
        if (entry == null)
            return Ok(); // Silently skip unavailable/unknown states

        var duplicate = await entryService.CheckForDuplicateEntryAsync(
            entry.Device, entry.Type, entry.Sgv, entry.Mills, cancellationToken: ct);

        if (duplicate != null)
            return Ok(); // Already have this reading

        await entryService.CreateEntriesAsync([entry], ct);
        return Ok();
    }
}
