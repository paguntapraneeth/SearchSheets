namespace SheetsSearchApp.Models;

// ─── Search request ────────────────────────────────────────────────────────

public sealed class SearchRequest
{
    /// <summary>Search terms. Minimum 2 characters.</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Maximum results to return. Range: 1–100. Default: 50.</summary>
    public int Limit { get; init; } = 50;

    /// <summary>Optional: restrict search to a specific sheet name.</summary>
    public string? SheetName { get; init; }
}

// ─── Search response ───────────────────────────────────────────────────────

public sealed class SearchResponse
{
    public string                    Query   { get; init; } = string.Empty;
    public int                       Count   { get; init; }
    public IReadOnlyList<SearchItem> Results { get; init; } = [];
}

/// <summary>
/// One search result. <see cref="Data"/> is the fully typed row object
/// (e.g. <see cref="ProductRow"/> or <see cref="CustomerRow"/>),
/// serialised as its concrete type so the API consumer sees all fields.
/// </summary>
public sealed class SearchItem
{
    public string    SheetName { get; init; } = string.Empty;
    public string    SheetType { get; init; } = string.Empty;
    public int       RowIndex  { get; init; }
    public string    Snippet   { get; init; } = string.Empty;

    /// <summary>
    /// The typed row data. Consumers can discriminate on <see cref="SheetType"/>
    /// to deserialise into <see cref="ProductRow"/> or <see cref="CustomerRow"/>.
    /// </summary>
    public ISheetRow Data { get; init; } = null!;
}
