using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Options;
using SheetsSearchApp.Models;

using GoogleSheetsService = Google.Apis.Sheets.v4.SheetsService;

namespace SheetsSearchApp.Services;

public sealed class SheetsService : ISheetsService, IDisposable
{
    private readonly GoogleSheetsService    _client;
    private readonly ILogger<SheetsService> _logger;

    public SheetsService(
        IOptions<AppSettings>   settings,
        ILogger<SheetsService>  logger)
    {
        _logger = logger;
        var credPath = settings.Value.CredentialsFilePath;

        if (!File.Exists(credPath))
            throw new FileNotFoundException(
                $"Google service-account credentials not found at: {Path.GetFullPath(credPath)}\n" +
                "Steps:\n" +
                "  1. Google Cloud Console → IAM → Service Accounts\n" +
                "  2. Create / select a service account → Keys → Add Key → JSON\n" +
                "  3. Save the downloaded file as credentials.json beside the app\n" +
                "  4. Share each spreadsheet with the service account email (Viewer role)\n" +
                "  5. Update CredentialsFilePath in appsettings.json if saved elsewhere");

        var credential = GoogleCredential
            .FromFile(credPath)
            .CreateScoped(GoogleSheetsService.Scope.SpreadsheetsReadonly);

        _client = new GoogleSheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName       = "SheetsSearchApp"
        });
    }

    public async Task<IList<ISheetRow>> GetRowsAsync(
        SheetConfig config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config.SheetType) ||
            !SheetRowFactory.IsKnownType(config.SheetType))
        {
            throw new InvalidOperationException(
                $"Sheet '{config.SheetId}' has an unknown SheetType '{config.SheetType}'. " +
                $"Set SheetType to one of: Product, Customer.");
        }

        var range = string.IsNullOrWhiteSpace(config.WorksheetName)
            ? "A1:ZZZ"
            : $"'{config.WorksheetName}'!A1:ZZZ";

        _logger.LogDebug(
            "Fetching {SheetId} [{SheetType}] range '{Range}'",
            config.SheetId, config.SheetType, range);

        var request  = _client.Spreadsheets.Values.Get(config.SheetId, range);
        var response = await request.ExecuteAsync(ct);
        var raw      = response.Values;

        if (raw is null || raw.Count < 2)
        {
            _logger.LogWarning(
                "Sheet {SheetId} returned no data rows (check that the first row is a header)",
                config.SheetId);
            return [];
        }

        // Row 0 = headers, rows 1..N = data.
        var headers = raw[0]
            .Select(h => h?.ToString() ?? string.Empty)
            .ToList();

        var result = new List<ISheetRow>(raw.Count - 1);

        for (var i = 1; i < raw.Count; i++)
        {
            var cells = raw[i];

            // Build column → value dictionary for this row.
            var dict = new Dictionary<string, object?>(headers.Count);
            for (var j = 0; j < headers.Count; j++)
                dict[headers[j]] = j < cells.Count ? cells[j]?.ToString() : null;

            try
            {
                result.Add(SheetRowFactory.Create(config.SheetType!, dict));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Skipping row {Row} in sheet {SheetId} — mapping failed",
                    i, config.SheetId);
            }
        }

        return result;
    }

    public void Dispose() => _client.Dispose();
}
