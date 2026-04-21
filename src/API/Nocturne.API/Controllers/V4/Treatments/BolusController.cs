using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nocturne.API.Controllers.V4.Base;
using Nocturne.API.Models.Requests.V4;
using Nocturne.Core.Contracts.V4.Repositories;
using Nocturne.Core.Models.V4;

namespace Nocturne.API.Controllers.V4.Treatments;

/// <summary>
/// Controller for managing insulin bolus records.
/// Exposes standard V4 CRUD operations via <see cref="V4CrudControllerBase{TModel,TCreateRequest,TUpdateRequest,TRepository}"/>.
/// </summary>
/// <remarks>
/// The <c>GET /</c> list endpoint is cached for 90 seconds (varying by all query string parameters).
///
/// On update, immutable fields (<see cref="Bolus.BolusType"/>, <see cref="Bolus.Kind"/>,
/// <see cref="Bolus.LegacyId"/>, <see cref="Bolus.CreatedAt"/>, <see cref="Bolus.PumpRecordId"/>,
/// <see cref="Bolus.DeviceId"/>, and <see cref="Bolus.AdditionalProperties"/>) are preserved from the
/// existing record. <see cref="Bolus.CorrelationId"/> falls back to the existing value if the request
/// does not supply one.
/// </remarks>
/// <seealso cref="IBolusRepository"/>
/// <seealso cref="Bolus"/>
/// <seealso cref="CreateBolusRequest"/>
/// <seealso cref="UpdateBolusRequest"/>
[ApiController]
[Route("api/v4/insulin/boluses")]
[Authorize]
[Produces("application/json")]
public class BolusController(IBolusRepository repo)
    : V4CrudControllerBase<Bolus, CreateBolusRequest, UpdateBolusRequest, IBolusRepository>(repo)
{
    /// <inheritdoc/>
    /// <remarks>Response is cached for 90 seconds, varying by all query parameters.</remarks>
    [ResponseCache(Duration = 90, VaryByQueryKeys = new[] { "*" })]
    public override Task<ActionResult<PaginatedResponse<Bolus>>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery] string sort = "timestamp_desc",
        [FromQuery] string? device = null, [FromQuery] string? source = null,
        CancellationToken ct = default)
        => base.GetAll(from, to, limit, offset, sort, device, source, ct);

    /// <summary>Maps a <see cref="CreateBolusRequest"/> to a new <see cref="Bolus"/> domain model.</summary>
    /// <param name="request">The inbound create request.</param>
    /// <returns>A new <see cref="Bolus"/> with all fields populated from the request. <see cref="Bolus.CorrelationId"/> defaults to a new UUID v7 when not supplied.</returns>
    protected override Bolus MapCreateToModel(CreateBolusRequest request) => new()
    {
        Timestamp = request.Timestamp.UtcDateTime,
        UtcOffset = request.UtcOffset,
        Device = request.Device,
        App = request.App,
        DataSource = request.DataSource,
        Insulin = request.Insulin,
        Programmed = request.Programmed,
        Delivered = request.Delivered,
        BolusType = request.BolusType,
        Kind = request.Kind,
        Automatic = request.Automatic,
        Duration = request.Duration,
        SyncIdentifier = request.SyncIdentifier,
        InsulinType = request.InsulinType,
        Unabsorbed = request.Unabsorbed,
        BolusCalculationId = request.BolusCalculationId,
        ApsSnapshotId = request.ApsSnapshotId,
        CorrelationId = request.CorrelationId ?? Guid.CreateVersion7(),
    };

    /// <summary>Maps an <see cref="UpdateBolusRequest"/> onto a <see cref="Bolus"/> domain model, preserving immutable fields from the existing record.</summary>
    /// <param name="id">The bolus ID to carry forward.</param>
    /// <param name="request">The inbound update request.</param>
    /// <param name="existing">The existing <see cref="Bolus"/> record; immutable fields (<c>BolusType</c>, <c>Kind</c>, <c>LegacyId</c>, <c>CreatedAt</c>, <c>PumpRecordId</c>, <c>DeviceId</c>, <c>AdditionalProperties</c>) are copied from here.</param>
    /// <returns>A fully-populated <see cref="Bolus"/> ready for persistence.</returns>
    protected override Bolus MapUpdateToModel(Guid id, UpdateBolusRequest request, Bolus existing) => new()
    {
        Id = id,
        Timestamp = request.Timestamp.UtcDateTime,
        UtcOffset = request.UtcOffset,
        Device = request.Device,
        App = request.App,
        DataSource = request.DataSource,
        Insulin = request.Insulin,
        Programmed = request.Programmed,
        Delivered = request.Delivered,
        BolusType = existing.BolusType,
        Kind = existing.Kind,
        Automatic = request.Automatic,
        Duration = request.Duration,
        SyncIdentifier = request.SyncIdentifier,
        InsulinType = request.InsulinType,
        Unabsorbed = request.Unabsorbed,
        BolusCalculationId = request.BolusCalculationId,
        ApsSnapshotId = request.ApsSnapshotId,
        CorrelationId = request.CorrelationId ?? existing.CorrelationId,
        LegacyId = existing.LegacyId,
        CreatedAt = existing.CreatedAt,
        PumpRecordId = existing.PumpRecordId,
        DeviceId = existing.DeviceId,
        AdditionalProperties = existing.AdditionalProperties,
    };
}
