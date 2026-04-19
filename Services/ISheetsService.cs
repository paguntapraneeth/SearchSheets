using SheetsSearchApp.Models;

namespace SheetsSearchApp.Services;

public interface ISheetsService
{
    /// <summary>
    /// Fetches raw rows from Google Sheets and maps them to typed
    /// <see cref="ISheetRow"/> instances using <see cref="SheetRowFactory"/>.
    /// </summary>
    Task<IList<ISheetRow>> GetRowsAsync(
        SheetConfig config, CancellationToken ct = default);
}
