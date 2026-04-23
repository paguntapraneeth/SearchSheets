using SheetsSearchApp.Models;

namespace SheetsSearchApp.Services;

public interface ISearchService
{
    Task<List<SearchItem>> SearchByPhoneAsync(string phone, CancellationToken ct = default);
    Task UpsertSheetRowsAsync(
        string sheetId,
        string sheetName,
        string sheetType,
        IList<ISheetRow> rows,
        CancellationToken ct = default);
}
