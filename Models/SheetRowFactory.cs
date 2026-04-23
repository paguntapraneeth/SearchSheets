namespace SheetsSearchApp.Models;

public static class SheetRowFactory
{
    public static ISheetRow Create(
        string sheetType,
        IDictionary<string, object?> rawRow)
    {
        if (string.IsNullOrWhiteSpace(sheetType))
            throw new InvalidOperationException("SheetType is required");

        var type = sheetType.Trim().ToLowerInvariant();

        return type switch
        {
            "appointment" => AppointmentRow.FromRow(rawRow),
            _ => throw new NotSupportedException(
                    $"Unknown SheetType '{sheetType}'. Supported: appointment")
        };
    }

    public static bool IsKnownType(string? sheetType)
    {
        if (string.IsNullOrWhiteSpace(sheetType))
            return false;

        return sheetType.Trim().ToLowerInvariant() == "appointment";
    }
}
