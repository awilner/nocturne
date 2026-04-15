using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenApi.Remote.Attributes;
using Nocturne.Core.Models.V4;

namespace Nocturne.API.Controllers.V4;

/// <summary>
/// Controller for browsing the static device catalog.
/// </summary>
[ApiController]
[Route("api/v4/devices")]
[Tags("V4 Devices")]
[Authorize]
public class DeviceCatalogController : ControllerBase
{
    /// <summary>
    /// Get all known device models.
    /// </summary>
    [HttpGet("catalog")]
    [RemoteQuery]
    public ActionResult<IReadOnlyList<DeviceCatalogEntry>> GetCatalog()
    {
        return Ok(DeviceCatalog.GetAll());
    }

    /// <summary>
    /// Get device models filtered by category.
    /// </summary>
    [HttpGet("catalog/{category}")]
    public ActionResult<IReadOnlyList<DeviceCatalogEntry>> GetCatalogByCategory(DeviceCategory category)
    {
        return Ok(DeviceCatalog.GetByCategory(category));
    }
}
