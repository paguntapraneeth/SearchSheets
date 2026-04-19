namespace SheetsSearchApp.Models;

/// <summary>
/// Marker interface for all typed sheet row models.
/// Allows the search pipeline to treat rows generically
/// while still carrying their concrete type through to the API response.
/// </summary>
public interface ISheetRow
{
    /// <summary>Returns the concatenated text that FTS5 should index.</summary>
    string ToSearchText();
}
