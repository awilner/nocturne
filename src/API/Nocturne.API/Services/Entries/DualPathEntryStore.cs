using Nocturne.Core.Contracts.V4;
using Nocturne.Core.Contracts.Entries;
using Nocturne.Core.Contracts.Repositories;
using Nocturne.Core.Models;
using Nocturne.Core.Models.Entries;
using Nocturne.API.Services.Glucose;
using Nocturne.API.Services.Platform;

namespace Nocturne.API.Services.Entries;

/// <summary>
/// Read-only <see cref="IEntryStore"/> that merges legacy <see cref="Entry"/> records with
/// V4-projected entries from <see cref="IV4ToLegacyProjectionService"/>.
/// Handles demo mode filtering via <see cref="IDemoModeService"/>, dual-path merge and
/// deduplication via <see cref="EntryDomainLogic"/>, and per-query projection decisions.
/// </summary>
/// <remarks>
/// For queries without date-string or reverse-result requirements, the store fetches
/// <c>count + skip</c> records from both paths, merges them by timestamp, deduplicates
/// by ID, then applies the final skip and take. This ensures correct pagination across sources.
/// </remarks>
/// <seealso cref="IEntryStore"/>
/// <seealso cref="IV4ToLegacyProjectionService"/>
/// <seealso cref="IDemoModeService"/>
/// <seealso cref="EntryService"/>
public class DualPathEntryStore : IEntryStore
{
    private readonly IEntryRepository _entryRepository;
    private readonly IV4ToLegacyProjectionService _projection;
    private readonly IDemoModeService _demoMode;
    private readonly ILogger<DualPathEntryStore> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DualPathEntryStore"/>.
    /// </summary>
    /// <param name="entryRepository">The legacy entry repository for data access.</param>
    /// <param name="projection">Service for projecting V4 typed records back to legacy <see cref="Entry"/> shape.</param>
    /// <param name="demoMode">Demo mode service that injects synthetic data filters when enabled.</param>
    /// <param name="logger">The logger instance.</param>
    public DualPathEntryStore(
        IEntryRepository entryRepository,
        IV4ToLegacyProjectionService projection,
        IDemoModeService demoMode,
        ILogger<DualPathEntryStore> logger)
    {
        _entryRepository = entryRepository;
        _projection = projection;
        _demoMode = demoMode;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Entry>> QueryAsync(EntryQuery query, CancellationToken ct = default)
    {
        var findQuery = EntryDomainLogic.BuildDemoModeFilterQuery(_demoMode.IsEnabled, query.Find);
        var type = query.Type;
        var count = query.Count;
        var skip = query.Skip;

        // When DateString or ReverseResults are specified, use the advanced filter directly
        if (query.DateString is not null || query.ReverseResults)
        {
            var entries = await _entryRepository.GetEntriesWithAdvancedFilterAsync(
                type, count, skip, findQuery, query.DateString, query.ReverseResults, ct);
            return entries.ToList();
        }

        var shouldProject = EntryDomainLogic.ShouldProject(type);
        var (fromMills, toMills) = EntryDomainLogic.ParseTimeRangeFromFind(query.Find);

        // Fetch from skip=0 so the merge can correctly interleave legacy and projected
        // entries before applying the final skip.
        var fetchCount = count + skip;

        var legacyEntries = await _entryRepository.GetEntriesWithAdvancedFilterAsync(
            type: type ?? "sgv",
            count: fetchCount,
            skip: 0,
            findQuery: findQuery,
            cancellationToken: ct);

        if (!shouldProject)
            return legacyEntries.Skip(skip).Take(count).ToList();

        var projectedEntries = await _projection.GetProjectedEntriesAsync(
            fromMills, toMills, fetchCount, 0, descending: true, ct);

        return EntryDomainLogic.MergeAndDeduplicate(legacyEntries, projectedEntries, count, skip);
    }

    public async Task<Entry?> GetCurrentAsync(CancellationToken ct = default)
    {
        var findQuery = EntryDomainLogic.BuildDemoModeFilterQuery(_demoMode.IsEnabled, null);

        // Fetch from legacy entries table and V4 projection sequentially.
        // They share a scoped DbContext which is not thread-safe for concurrent access.
        var legacyEntry = (await _entryRepository.GetEntriesWithAdvancedFilterAsync(
            type: "sgv",
            count: 1,
            skip: 0,
            findQuery: findQuery,
            cancellationToken: ct
        )).FirstOrDefault();

        var projectedEntry = await _projection.GetLatestProjectedEntryAsync(ct);

        return EntryDomainLogic.SelectMostRecent(legacyEntry, projectedEntry);
    }

    public async Task<Entry?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _entryRepository.GetEntryByIdAsync(id, ct);
    }

    public async Task<Entry?> CheckDuplicateAsync(string? device, string type, double? sgv, long mills,
        int windowMinutes = 5, CancellationToken ct = default)
    {
        return await _entryRepository.CheckForDuplicateEntryAsync(device, type, sgv, mills, windowMinutes, ct);
    }
}
