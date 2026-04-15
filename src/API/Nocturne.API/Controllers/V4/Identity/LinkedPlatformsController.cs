using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenApi.Remote.Attributes;
using Nocturne.API.Services.Chat;
using Nocturne.Core.Contracts.Multitenancy;

namespace Nocturne.API.Controllers.V4.Identity;

[ApiController]
[Authorize]
[Route("api/v4/chat-identity")]
public class LinkedPlatformsController(
    ChatIdentityDirectoryService directory,
    ITenantAccessor tenantAccessor) : ControllerBase
{
    [HttpGet("linked-platforms")]
    [RemoteQuery]
    [ProducesResponseType(typeof(LinkedPlatformsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LinkedPlatformsResponse>> GetLinkedPlatforms(
        CancellationToken ct)
    {
        var entries = await directory.GetByTenantAsync(tenantAccessor.TenantId, ct);
        var platforms = entries.Select(e => e.Platform).Distinct().ToList();
        return Ok(new LinkedPlatformsResponse { Platforms = platforms });
    }
}

public class LinkedPlatformsResponse
{
    public IReadOnlyList<string> Platforms { get; set; } = [];
}
