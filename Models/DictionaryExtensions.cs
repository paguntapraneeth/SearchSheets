namespace SheetsSearchApp.Models;

/// <summary>
/// Safe read helpers for mapping a raw Sheets API row
/// (IDictionary&lt;string, object?&gt;) to typed properties.
/// Missing or null cells return sensible defaults instead of throwing.
/// </summary>
public static class DictionaryExtensions
{
    public static string GetString(
        this IDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var v) ? v?.ToString()?.Trim() ?? string.Empty
                                           : string.Empty;

    public static string? GetNullableString(
        this IDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v)) return null;
        var s = v?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    public static decimal? GetDecimal(
        this IDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v)) return null;
        var s = v?.ToString()?.Trim().TrimStart('$', '£', '€', '₹');
        return decimal.TryParse(s, out var d) ? d : null;
    }

    public static int? GetInt(
        this IDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v)) return null;
        return int.TryParse(v?.ToString()?.Trim(), out var i) ? i : null;
    }

    public static bool? GetBool(
        this IDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v)) return null;
        var s = v?.ToString()?.Trim().ToLowerInvariant();
        return s is "true" or "yes" or "1" ? true
             : s is "false" or "no"  or "0" ? false
             : null;
    }

    public static DateTime? GetDateTime(
        this IDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v)) return null;
        return DateTime.TryParse(v?.ToString(), out var d) ? d : null;
    }
}
