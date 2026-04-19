using SheetsSearchApp.Models;

namespace SheetsSearchApp.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(
        SearchRequest request, CancellationToken ct = default);

    Task UpsertSheetRowsAsync(
        string sheetId,
        string sheetName,
        string sheetType,
        IList<ISheetRow> rows,
        CancellationToken ct = default);

    Task<SyncStatusResponse> GetStatusAsync(CancellationToken ct = default);
}
