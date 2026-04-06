using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nocturne.Connectors.HomeAssistant.Configurations;
using Nocturne.Connectors.HomeAssistant.Mappers;
using Nocturne.Connectors.HomeAssistant.Models;
using Nocturne.Core.Contracts;

namespace Nocturne.API.Controllers.V4.Connectors;

/// <summary>
/// Receives inbound webhooks from Home Assistant automations.
/// Authentication is via secret in URL path, not standard auth middleware.
/// </summary>
[ApiController]
[Route("api/v4/connectors/home-assistant/webhook")]
[AllowAnonymous]
public class HomeAssistantWebhookController(
    HomeAssistantConnectorConfiguration config,
    HomeAssistantEntityMapper mapper,
    IEntryService entryService,
    ILogger<HomeAssistantWebhookController> logger) : ControllerBase
{
    [HttpPost("{secret}")]
    public async Task<IActionResult> ReceiveWebhook(
        string secret,
        [FromBody] HomeAssistantStateResponse payload,
        CancellationToken ct)
    {
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

        // Currently only glucose mapping implemented
        var entry = mapper.MapToEntry(payload);
        if (entry == null)
            return Ok(); // Silently skip unavailable/unknown states

        // Check for duplicate before persisting
        var duplicate = await entryService.CheckForDuplicateEntryAsync(
            entry.Device, entry.Type, entry.Sgv, entry.Mills, cancellationToken: ct);

        if (duplicate != null)
            return Ok(); // Already have this reading

        await entryService.CreateEntriesAsync([entry], ct);

        return Ok();
    }
}
