using Microsoft.EntityFrameworkCore;
using Nocturne.Core.Contracts.V4;
using Nocturne.Core.Models;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Mappers;

namespace Nocturne.API.Services.V4;

/// <summary>
/// Backfills existing legacy entries and treatments into v4 granular tables by streaming records
/// in batches via a composite cursor (<c>Mills</c>, <c>Id</c>) and delegating each batch to
/// <see cref="IDecompositionPipeline"/>.
/// </summary>
/// <remarks>
/// Because the underlying decomposers use <c>LegacyId</c> for idempotent create-or-update,
/// re-running the backfill is safe — existing v4 records are updated rather than duplicated.
/// Temp-basal and profile-switch treatments are intentionally skipped during backfill because they
/// are already represented as <see cref="StateSpan"/> records written by the live decomposer.
/// </remarks>
/// <seealso cref="IDecompositionPipeline"/>
public class V4BackfillService
{
    private readonly IDecompositionPipeline _pipeline;
    private readonly NocturneDbContext _context;
    private readonly ILogger<V4BackfillService> _logger;

    private const int BatchSize = 1000;

    /// <summary>
    /// Event types that should be skipped during treatment backfill because they are
    /// already handled as StateSpans (temp basals and profile switches).
    /// </summary>
    private static readonly string[] SkippedEventTypes =
    [
        "Temp Basal",
        "Temp Basal Start",
        "TempBasal",
        "Profile Switch",
    ];

    /// <param name="pipeline">Decomposition pipeline that dispatches records to the appropriate decomposer.</param>
    /// <param name="context">EF Core context used to read from the legacy entries and treatments tables.</param>
    /// <param name="logger">Logger instance for this service.</param>
    public V4BackfillService(
        IDecompositionPipeline pipeline,
        NocturneDbContext context,
        ILogger<V4BackfillService> logger
    )
    {
        _pipeline = pipeline;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Backfills all existing entries and treatments into the v4 granular tables.
    /// Processes records in batches ordered by Mills ascending.
    /// </summary>
    public async Task<BackfillResult> BackfillAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("V4 backfill started");

        var result = new BackfillResult();

        await BackfillEntriesAsync(result, ct);
        await BackfillTreatmentsAsync(result, ct);

        _logger.LogInformation(
            "V4 backfill completed. Entries: {EntriesProcessed} processed, {EntriesFailed} failed. Treatments: {TreatmentsProcessed} processed, {TreatmentsFailed} failed, {TreatmentsSkipped} skipped",
            result.EntriesProcessed,
            result.EntriesFailed,
            result.TreatmentsProcessed,
            result.TreatmentsFailed,
            result.TreatmentsSkipped
        );

        return result;
    }

    private async Task BackfillEntriesAsync(BackfillResult result, CancellationToken ct)
    {
        var totalEntries = await _context.Entries.CountAsync(ct);
        _logger.LogInformation("Backfill entries: {Total} total entries to process", totalEntries);

        if (totalEntries == 0)
            return;

        long lastMills = long.MinValue;
        Guid lastId = Guid.Empty;
        var processed = 0;

        while (!ct.IsCancellationRequested)
        {
            // Use composite cursor (Mills, Id) to handle records with identical Mills values
            var batch = await _context
                .Entries.AsNoTracking()
                .Where(e =>
                    e.Mills > lastMills || (e.Mills == lastMills && e.Id.CompareTo(lastId) > 0)
                )
                .OrderBy(e => e.Mills)
                .ThenBy(e => e.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            var entries = batch.Select(EntryMapper.ToDomainModel).ToList();
            var batchResult = await _pipeline.DecomposeAsync<Entry>(entries, ct);
            result.EntriesProcessed += batchResult.Succeeded;
            result.EntriesFailed += batchResult.Failed;

            lastMills = batch[^1].Mills;
            lastId = batch[^1].Id;
            processed += batch.Count;

            _logger.LogInformation(
                "Backfill entries: processed {Count}/{Total}",
                processed,
                totalEntries
            );

            if (batch.Count < BatchSize)
                break;
        }
    }

    private async Task BackfillTreatmentsAsync(BackfillResult result, CancellationToken ct)
    {
        var totalTreatments = await _context.Treatments.CountAsync(ct);
        _logger.LogInformation(
            "Backfill treatments: {Total} total treatments to process",
            totalTreatments
        );

        if (totalTreatments == 0)
            return;

        long lastMills = long.MinValue;
        Guid lastId = Guid.Empty;
        var processed = 0;

        while (!ct.IsCancellationRequested)
        {
            // Use composite cursor (Mills, Id) to handle records with identical Mills values
            var batch = await _context
                .Treatments.AsNoTracking()
                .Where(t =>
                    t.Mills > lastMills || (t.Mills == lastMills && t.Id.CompareTo(lastId) > 0)
                )
                .OrderBy(t => t.Mills)
                .ThenBy(t => t.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            // Skip TempBasal and ProfileSwitch treatments — these are already
            // handled as StateSpans by the decomposer for new writes
            var treatments = new List<Treatment>();
            foreach (var entity in batch)
            {
                var treatment = TreatmentMapper.ToDomainModel(entity);
                if (ShouldSkipTreatment(treatment))
                    result.TreatmentsSkipped++;
                else
                    treatments.Add(treatment);
            }

            var batchResult = await _pipeline.DecomposeAsync<Treatment>(treatments, ct);
            result.TreatmentsProcessed += batchResult.Succeeded;
            result.TreatmentsFailed += batchResult.Failed;

            lastMills = batch[^1].Mills;
            lastId = batch[^1].Id;
            processed += batch.Count;

            _logger.LogInformation(
                "Backfill treatments: processed {Count}/{Total}",
                processed,
                totalTreatments
            );

            if (batch.Count < BatchSize)
                break;
        }
    }

    /// <summary>
    /// Determines if a treatment should be skipped during backfill.
    /// Temp basals and profile switches are already represented as StateSpans.
    /// </summary>
    private static bool ShouldSkipTreatment(Treatment treatment)
    {
        if (string.IsNullOrEmpty(treatment.EventType))
            return false;

        return SkippedEventTypes.Any(eventType =>
            string.Equals(treatment.EventType, eventType, StringComparison.OrdinalIgnoreCase)
        );
    }
}

/// <summary>
/// Result of a V4 backfill operation
/// </summary>
public class BackfillResult
{
    /// <summary>
    /// Number of entries successfully decomposed into v4 records
    /// </summary>
    public long EntriesProcessed { get; set; }

    /// <summary>
    /// Number of entries that failed decomposition
    /// </summary>
    public long EntriesFailed { get; set; }

    /// <summary>
    /// Number of treatments successfully decomposed into v4 records
    /// </summary>
    public long TreatmentsProcessed { get; set; }

    /// <summary>
    /// Number of treatments that failed decomposition
    /// </summary>
    public long TreatmentsFailed { get; set; }

    /// <summary>
    /// Number of treatments skipped (temp basals, profile switches)
    /// </summary>
    public long TreatmentsSkipped { get; set; }
}
