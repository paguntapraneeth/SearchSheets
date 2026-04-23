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

public class SearchItem
{
    public string? SheetName { get; set; }
    public string? SheetType { get; set; }
    public int RowIndex { get; set; }

    // 🔥 Flattened fields
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? TH { get; set; }
    public string? RO { get; set; }
    public decimal? AM { get; set; }
}
