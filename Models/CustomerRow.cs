namespace SheetsSearchApp.Models;

/// <summary>
/// Represents one row from the Customers Google Sheet.
/// Column names in <see cref="FromRow"/> must match your sheet's header row exactly.
/// </summary>
public sealed class CustomerRow : ISheetRow
{
    public string    CustomerId  { get; init; } = string.Empty;
    public string    FullName    { get; init; } = string.Empty;
    public string    Email       { get; init; } = string.Empty;
    public string?   Phone       { get; init; }
    public string?   Company     { get; init; }
    public string?   Region      { get; init; }
    public string?   Country     { get; init; }
    public string?   Segment     { get; init; }
    public DateTime? JoinedAt    { get; init; }
    public bool?     IsActive    { get; init; }

    /// <summary>
    /// Maps a raw dictionary row (from the Sheets API) to a <see cref="CustomerRow"/>.
    /// Keys must match the header row in your spreadsheet.
    /// </summary>
    public static CustomerRow FromRow(IDictionary<string, object?> row) => new()
    {
        CustomerId = row.GetString("Customer ID"),
        FullName   = row.GetString("Full Name"),
        Email      = row.GetString("Email"),
        Phone      = row.GetNullableString("Phone"),
        Company    = row.GetNullableString("Company"),
        Region     = row.GetNullableString("Region"),
        Country    = row.GetNullableString("Country"),
        Segment    = row.GetNullableString("Segment"),
        JoinedAt   = row.GetDateTime("Joined Date"),
        IsActive   = row.GetBool("Active"),
    };

    /// <summary>
    /// Builds the searchable text blob stored in the FTS5 index.
    /// </summary>
    public string ToSearchText() =>
        string.Join(" ",
            new[] { CustomerId, FullName, Email, Phone, Company, Region, Country, Segment }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
