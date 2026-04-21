using Microsoft.AspNetCore.Mvc;
using Nocturne.API.Attributes;
using Nocturne.API.Authorization;
using Nocturne.API.Services.V4;

namespace Nocturne.API.Controllers.V4.TenantAdmin;

/// <summary>
/// Admin controller for triggering V4 backfill operations.
/// Decomposes all existing legacy entries and treatments into the v4 granular tables.
/// </summary>
/// <seealso cref="V4BackfillService"/>
[ApiController]
[Route("api/v4/admin")]
[RequireAdmin]
[Produces("application/json")]
[AllowDuringSetup]
public class BackfillController : ControllerBase
{
    private readonly V4BackfillService _backfillService;
    private readonly ILogger<BackfillController> _logger;
    private static readonly SemaphoreSlim BackfillLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of <see cref="BackfillController"/>.
    /// </summary>
    /// <param name="backfillService">Service that performs the V4 data backfill operation.</param>
    /// <param name="logger">Logger instance.</param>
    public BackfillController(
        V4BackfillService backfillService,
        ILogger<BackfillController> logger)
    {
        _backfillService = backfillService;
        _logger = logger;
    }

    /// <summary>
    /// Triggers a backfill operation to reprocess all legacy entries and treatments into V4 granular tables.
    /// Only one backfill may run at a time; concurrent calls return 409 Conflict.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="BackfillResult"/> with processed counts on success;
    /// 409 if already running; 500 on internal error.
    /// </returns>
    [HttpPost("backfill")]
    [ProducesResponseType(typeof(BackfillResult), 200)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BackfillResult>> TriggerBackfill(CancellationToken ct)
    {
        if (!await BackfillLock.WaitAsync(0, ct))
        {
            return Problem(detail: "A backfill operation is already in progress", statusCode: 409, title: "Conflict");
        }

        try
        {
            _logger.LogInformation("V4 backfill triggered via admin endpoint");
            var result = await _backfillService.BackfillAsync(ct);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("V4 backfill was cancelled");
            return StatusCode(499, new { status = "cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V4 backfill failed");
            return Problem(detail: ex.Message, statusCode: 500, title: "Internal Server Error");
        }
        finally
        {
            BackfillLock.Release();
        }
    }
}
