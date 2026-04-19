namespace SheetsSearchApp.Models;

public sealed class SheetConfig
{
    /// <summary>The long ID from the Google Sheets URL.</summary>
    public string  SheetId       { get; init; } = string.Empty;

    /// <summary>Optional: pull a specific worksheet tab by name. Defaults to the first tab.</summary>
    public string? WorksheetName { get; init; }

    /// <summary>
    /// Maps this sheet to a known domain type.
    /// Supported values: "Product", "Customer"
    /// Leave empty to fall back to the generic dictionary renderer.
    /// </summary>
    public string? SheetType     { get; init; }

    /// <summary>Human-readable label shown in the UI and search results.</summary>
    public string? DisplayName   { get; init; }
}
