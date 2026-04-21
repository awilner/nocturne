using Nocturne.Core.Contracts;
using Nocturne.Core.Contracts.Entries;
using Nocturne.Core.Contracts.Events;
using Nocturne.Core.Contracts.Repositories;
using Nocturne.Core.Models;

namespace Nocturne.API.Services;

/// <summary>
/// Domain service implementation for <see cref="Entry"/> operations using Store/Cache/EventSink ports.
/// Delegates reads through <see cref="IEntryCache"/> with fallback to <see cref="IEntryStore"/>,
/// and writes through <see cref="IEntryRepository"/> with event notification via <see cref="IDataEventSink{T}"/>.
/// </summary>
/// <seealso cref="IEntryService"/>
/// <seealso cref="IEntryStore"/>
/// <seealso cref="IEntryRepository"/>
/// <seealso cref="IEntryCache"/>
public class EntryService : IEntryService
{
    private readonly IEntryStore _store;
    private readonly IEntryRepository _repository;
    private readonly IEntryCache _cache;
    private readonly IDataEventSink<Entry> _events;
    private readonly ILogger<EntryService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EntryService"/>.
    /// </summary>
    /// <param name="store">The entry store for query operations.</param>
    /// <param name="repository">The entry repository for write operations.</param>
    /// <param name="cache">The entry cache for read-through caching.</param>
    /// <param name="events">The event sink for broadcasting create/update/delete events.</param>
    /// <param name="logger">The logger instance.</param>
    public EntryService(
        IEntryStore store,
        IEntryRepository repository,
        IEntryCache cache,
        IDataEventSink<Entry> events,
        ILogger<EntryService> logger)
    {
        _store = store;
        _repository = repository;
        _cache = cache;
        _events = events;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Entry>> GetEntriesAsync(
        string? find = null,
        int? count = null,
        int? skip = null,
        CancellationToken cancellationToken = default)
    {
        var query = new EntryQuery
        {
            Find = find,
            Count = count ?? 10,
            Skip = skip ?? 0
        };

        var cached = await _cache.GetOrComputeAsync(
            query,
            () => _store.QueryAsync(query, cancellationToken),
            cancellationToken);

        return cached ?? await _store.QueryAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Entry>> GetEntriesAsync(
        string? type,
        int count,
        int skip,
        CancellationToken cancellationToken)
    {
        var query = new EntryQuery
        {
            Type = type,
            Count = count,
            Skip = skip
        };

        var cached = await _cache.GetOrComputeAsync(
            query,
            () => _store.QueryAsync(query, cancellationToken),
            cancellationToken);

        return cached ?? await _store.QueryAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Entry?> GetEntryByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await _store.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Entry?> CheckForDuplicateEntryAsync(
        string? device,
        string type,
        double? sgv,
        long mills,
        int windowMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        return await _store.CheckDuplicateAsync(device, type, sgv, mills, windowMinutes, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Entry?> GetCurrentEntryAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrComputeCurrentAsync(
            () => _store.GetCurrentAsync(cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Entry>> GetEntriesWithAdvancedFilterAsync(
        string find,
        int count,
        int skip,
        CancellationToken cancellationToken = default)
    {
        var query = new EntryQuery
        {
            Find = find,
            Count = count,
            Skip = skip
        };

        return await _store.QueryAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Entry>> GetEntriesWithAdvancedFilterAsync(
        string? type,
        int count,
        int skip,
        string? findQuery,
        string? dateString,
        bool reverseResults,
        CancellationToken cancellationToken = default)
    {
        var query = new EntryQuery
        {
            Type = type,
            Find = findQuery,
            Count = count,
            Skip = skip,
            DateString = dateString,
            ReverseResults = reverseResults
        };

        return await _store.QueryAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Persists entries via <see cref="IEntryRepository.CreateEntriesAsync"/> and then
    /// fires <see cref="IDataEventSink{T}.OnCreatedAsync"/> to trigger cache invalidation
    /// and SignalR broadcasting.
    /// </remarks>
    public async Task<IEnumerable<Entry>> CreateEntriesAsync(
        IEnumerable<Entry> entries,
        CancellationToken cancellationToken = default)
    {
        var created = (await _repository.CreateEntriesAsync(entries.ToList(), cancellationToken)).ToList();

        await _events.OnCreatedAsync(created, cancellationToken);

        return created;
    }

    /// <inheritdoc />
    /// <returns>The updated <see cref="Entry"/>, or <see langword="null"/> if no entry with the given <paramref name="id"/> exists.</returns>
    public async Task<Entry?> UpdateEntryAsync(
        string id,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateEntryAsync(id, entry, cancellationToken);
        if (updated is null) return null;

        await _events.OnUpdatedAsync(updated, cancellationToken);

        return updated;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Fires <see cref="IDataEventSink{T}.BeforeDeleteAsync"/> before deletion to allow
    /// V4 decomposition cleanup, then removes the entry and broadcasts the deletion event.
    /// </remarks>
    public async Task<bool> DeleteEntryAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _events.BeforeDeleteAsync(id, cancellationToken);

        var entryToDelete = await _store.GetByIdAsync(id, cancellationToken);
        var deleted = await _repository.DeleteEntryAsync(id, cancellationToken);

        if (deleted)
        {
            await _events.OnDeletedAsync(entryToDelete, cancellationToken);
        }

        return deleted;
    }

    /// <inheritdoc />
    /// <returns>The number of entries deleted.</returns>
    public async Task<long> DeleteEntriesAsync(
        string? find = null,
        CancellationToken cancellationToken = default)
    {
        var deletedCount = await _repository.BulkDeleteEntriesAsync(find ?? "{}", cancellationToken);

        await _events.OnBulkDeletedAsync(deletedCount, cancellationToken);

        return deletedCount;
    }
}
