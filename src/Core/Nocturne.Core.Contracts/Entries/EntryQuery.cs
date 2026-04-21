namespace Nocturne.Core.Contracts.Entries;

/// <summary>
/// Value object encapsulating query parameters for entry reads.
/// Used by <see cref="IEntryStore"/> and <see cref="IEntryCache"/> to filter and paginate results.
/// </summary>
/// <seealso cref="IEntryStore"/>
/// <seealso cref="IEntryCache"/>
public sealed record EntryQuery
{
    /// <summary>
    /// Optional Nightscout-compatible <c>find</c> query string for server-side filtering
    /// (e.g., <c>find[sgv][$gte]=180</c>).
    /// </summary>
    public string? Find { get; init; }

    /// <summary>
    /// Optional entry type filter (e.g., "sgv", "mbg", "cal").
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Maximum number of entries to return. Defaults to 10.
    /// </summary>
    public int Count { get; init; } = 10;

    /// <summary>
    /// Number of entries to skip for pagination. Defaults to 0.
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Optional ISO-8601 date string used for date-range filtering.
    /// </summary>
    public string? DateString { get; init; }

    /// <summary>
    /// When <c>true</c>, results are returned in ascending chronological order
    /// instead of the default descending order.
    /// </summary>
    public bool ReverseResults { get; init; } = false;
}
