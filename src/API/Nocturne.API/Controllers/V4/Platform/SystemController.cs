using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenApi.Remote.Attributes;
using Nocturne.API.Services;
using Nocturne.Core.Models.Alerts;

namespace Nocturne.API.Controllers.V4.Platform;

[ApiController]
[Authorize]
[Route("api/v4/system")]
public class SystemController(BotHealthService botHealth) : ControllerBase
{
    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Heartbeat([FromBody] HeartbeatRequest request)
    {
        botHealth.Record(request.Platforms);
        return Ok();
    }

    [HttpGet("channels")]
    [RemoteQuery]
    [ProducesResponseType(typeof(ChannelStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<ChannelStatusResponse> GetChannelStatuses()
    {
        var statuses = botHealth.GetChannelStatuses();
        return Ok(new ChannelStatusResponse { Channels = statuses });
    }
}

public class HeartbeatRequest
{
    public string[] Platforms { get; set; } = [];
    public string Service { get; set; } = string.Empty;
}

public class ChannelStatusResponse
{
    public IReadOnlyList<ChannelStatusEntry> Channels { get; set; } = [];
}
