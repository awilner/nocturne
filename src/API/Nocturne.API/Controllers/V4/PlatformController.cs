using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenApi.Remote.Attributes;
using Nocturne.API.Multitenancy;
using Nocturne.Core.Contracts.Multitenancy;
using Nocturne.Core.Models.Authorization;

namespace Nocturne.API.Controllers.V4;

/// <summary>
/// Cross-tenant endpoints on the apex domain for authenticated users.
/// These operate without a resolved tenant context.
/// </summary>
[ApiController]
[Route("api/v4/platform")]
[Produces("application/json")]
[Authorize]
public class PlatformController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly MultitenancyConfiguration _config;

    public PlatformController(
        ITenantService tenantService,
        IOptions<MultitenancyConfiguration> config)
    {
        _tenantService = tenantService;
        _config = config.Value;
    }

    /// <summary>
    /// Returns all tenants the authenticated subject is a member of.
    /// </summary>
    [HttpGet("tenants")]
    [RemoteQuery]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTenants(CancellationToken ct)
    {
        var authContext = HttpContext.Items["AuthContext"] as AuthContext;
        if (authContext?.SubjectId == null)
            return Unauthorized();

        var tenants = await _tenantService.GetTenantsForSubjectAsync(authContext.SubjectId.Value, ct);
        return Ok(tenants);
    }

    /// <summary>
    /// Creates a new tenant with the authenticated subject as owner.
    /// Requires MultitenancyConfiguration.AllowSelfServiceCreation to be enabled.
    /// </summary>
    [HttpPost("tenants")]
    [RemoteCommand(Invalidates = ["GetTenants"])]
    [ProducesResponseType(typeof(TenantCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] CreatePlatformTenantRequest request, CancellationToken ct)
    {
        if (!_config.AllowSelfServiceCreation)
            return Forbid();

        var authContext = HttpContext.Items["AuthContext"] as AuthContext;
        if (authContext?.SubjectId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Slug) || string.IsNullOrWhiteSpace(request.DisplayName))
            return Problem(detail: "Slug and display name are required", statusCode: 400, title: "Bad Request");

        var validation = await _tenantService.ValidateSlugAsync(request.Slug, ct);
        if (!validation.IsValid)
            return Problem(detail: validation.Message, statusCode: 400, title: "Bad Request");

        var tenant = await _tenantService.CreateAsync(
            request.Slug, request.DisplayName, authContext.SubjectId.Value, ct: ct);

        return Created($"/api/v4/platform/tenants", tenant);
    }
}

public record CreatePlatformTenantRequest(string Slug, string DisplayName);
