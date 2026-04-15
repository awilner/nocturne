using Microsoft.EntityFrameworkCore;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Entities;
using Nocturne.Infrastructure.Data.Mappers;

namespace Nocturne.API.Services;

/// <summary>
/// Domain service for body weight record operations.
/// Inherits CRUD + broadcasting from SimpleEntityService.
/// </summary>
public class BodyWeightService
    : SimpleEntityService<BodyWeight, BodyWeightEntity>,
        IBodyWeightService
{
    public BodyWeightService(
        NocturneDbContext dbContext,
        IDocumentProcessingService documentProcessingService,
        ISignalRBroadcastService signalRBroadcastService,
        ILogger<BodyWeightService> logger
    )
        : base(dbContext, documentProcessingService, signalRBroadcastService, logger) { }

    protected override DbSet<BodyWeightEntity> EntitySet => DbContext.BodyWeights;
    protected override string CollectionName => "bodyweight";
    protected override string EntityTypeName => "body weight";

    protected override BodyWeight ToDomainModel(BodyWeightEntity entity) =>
        BodyWeightMapper.ToDomainModel(entity);

    protected override BodyWeightEntity ToEntity(BodyWeight model) =>
        BodyWeightMapper.ToEntity(model);

    protected override void UpdateEntity(BodyWeightEntity entity, BodyWeight model) =>
        BodyWeightMapper.UpdateEntity(entity, model);

    protected override IOrderedQueryable<BodyWeightEntity> OrderByTimestamp(
        IQueryable<BodyWeightEntity> query
    ) => query.OrderByDescending(b => b.Mills);

    protected override Task<BodyWeightEntity?> FindByIdAsync(
        string id,
        CancellationToken cancellationToken
    ) =>
        Guid.TryParse(id, out var guid)
            ? DbContext.BodyWeights.FirstOrDefaultAsync(b => b.Id == guid, cancellationToken)
            : DbContext.BodyWeights.FirstOrDefaultAsync(
                b => b.OriginalId == id,
                cancellationToken
            );

    public Task<IEnumerable<BodyWeight>> GetBodyWeightsAsync(
        int count = 10,
        int skip = 0,
        CancellationToken cancellationToken = default
    ) => GetAllAsync(count, skip, cancellationToken);

    public Task<BodyWeight?> GetBodyWeightByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    ) => GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<BodyWeight>> CreateBodyWeightsAsync(
        IEnumerable<BodyWeight> bodyWeights,
        CancellationToken cancellationToken = default
    ) => CreateManyAsync(bodyWeights, cancellationToken);

    public Task<BodyWeight?> UpdateBodyWeightAsync(
        string id,
        BodyWeight bodyWeight,
        CancellationToken cancellationToken = default
    ) => UpdateOneAsync(id, bodyWeight, cancellationToken);

    public Task<bool> DeleteBodyWeightAsync(
        string id,
        CancellationToken cancellationToken = default
    ) => DeleteOneAsync(id, cancellationToken);
}
