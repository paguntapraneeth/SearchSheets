namespace SheetsSearchApp.Models;

/// <summary>
/// Represents one row from the Products Google Sheet.
/// Column names in <see cref="FromRow"/> must match your sheet's header row exactly.
/// </summary>
public sealed class ProductRow : ISheetRow
{
    public string   ProductId   { get; init; } = string.Empty;
    public string   Name        { get; init; } = string.Empty;
    public string   Category    { get; init; } = string.Empty;
    public string   Description { get; init; } = string.Empty;
    public decimal? Price       { get; init; }
    public string?  Sku         { get; init; }
    public int?     StockQty    { get; init; }
    public bool?    IsActive    { get; init; }
    public string?  SupplierName { get; init; }
    public DateTime? LastUpdated { get; init; }

    /// <summary>
    /// Maps a raw dictionary row (from the Sheets API) to a <see cref="ProductRow"/>.
    /// Keys must match the header row in your spreadsheet.
    /// </summary>
    public static ProductRow FromRow(IDictionary<string, object?> row) => new()
    {
        ProductId    = row.GetString("Product ID"),
        Name         = row.GetString("Name"),
        Category     = row.GetString("Category"),
        Description  = row.GetString("Description"),
        Price        = row.GetDecimal("Price"),
        Sku          = row.GetNullableString("SKU"),
        StockQty     = row.GetInt("Stock Qty"),
        IsActive     = row.GetBool("Active"),
        SupplierName = row.GetNullableString("Supplier"),
        LastUpdated  = row.GetDateTime("Last Updated"),
    };

    /// <summary>
    /// Builds the searchable text blob stored in the FTS5 index.
    /// Include every field users might reasonably search by.
    /// </summary>
    public string ToSearchText() =>
        string.Join(" ",
            new[] { ProductId, Name, Category, Description, Sku, SupplierName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
