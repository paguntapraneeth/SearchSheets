namespace SheetsSearchApp.Models;

/// <summary>
/// Converts a raw Sheets API row dictionary into the correct typed <see cref="ISheetRow"/>
/// based on the <see cref="SheetConfig.SheetType"/> value.
///
/// To add a new sheet type:
///   1. Create a new class that implements <see cref="ISheetRow"/>
///   2. Add a case here
///   3. Register the SheetType string in appsettings.json
/// </summary>
public static class SheetRowFactory
{
    public static ISheetRow Create(
        string sheetType,
        IDictionary<string, object?> rawRow)
        => sheetType.Trim().ToLowerInvariant() switch
        {
            "product"  => ProductRow.FromRow(rawRow),
            "customer" => CustomerRow.FromRow(rawRow),
            _          => throw new NotSupportedException(
                              $"Unknown SheetType '{sheetType}'. " +
                              $"Supported values: Product, Customer.")
        };

    public static bool IsKnownType(string? sheetType)
        => sheetType?.Trim().ToLowerInvariant() is "product" or "customer";
}
