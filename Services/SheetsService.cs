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
    IOptions<AppSettings> settings,
    ILogger<SheetsService> logger)
    {
        _logger = logger;

        var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");

        GoogleCredential credential;

        if (!string.IsNullOrWhiteSpace(json))
        {
            // ✅ Render / production
            credential = GoogleCredential
                .FromJson(json)
                .CreateScoped(GoogleSheetsService.Scope.SpreadsheetsReadonly);
        }
        else
        {
            // ✅ Local fallback
            var credPath = "credentials.json";

            if (!File.Exists(credPath))
            {
                throw new InvalidOperationException(
                    "Neither GOOGLE_CREDENTIALS env variable nor credentials.json found.");
            }

            credential = GoogleCredential
                .FromFile(credPath)
                .CreateScoped(GoogleSheetsService.Scope.SpreadsheetsReadonly);
        }

        _client = new GoogleSheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SheetsSearchApp"
        });
    }

    public async Task<IList<ISheetRow>> GetRowsAsync(
    SheetConfig config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config.SheetType))
            throw new InvalidOperationException("SheetType is required");

        //  Correct range (FIXED)
        var worksheet = string.IsNullOrWhiteSpace(config.WorksheetName)
            ? "Sheet1"
            : config.WorksheetName;

        var range = $"{worksheet}!A1:M";   // 👈 based on your columns

        _logger.LogInformation(
            "Fetching {SheetId} [{SheetType}] range '{Range}'",
            config.SheetId, config.SheetType, range);

        var request = _client.Spreadsheets.Values.Get(config.SheetId, range);
        Console.WriteLine($"SheetId = {config.SheetId}");
        Console.WriteLine($"Worksheet = {config.WorksheetName}");
        Console.WriteLine($"Range = {range}");
        var response = await request.ExecuteAsync(ct);
        var raw = response.Values;

        if (raw is null || raw.Count < 2)
        {
            _logger.LogWarning("No data found in sheet {SheetId}", config.SheetId);
            return [];
        }

        var headers = raw[0]
            .Select(h => h?.ToString()?.Trim() ?? string.Empty)
            .ToList();

        var result = new List<ISheetRow>();

        for (var i = 1; i < raw.Count; i++)
        {
            var cells = raw[i];

            var dict = new Dictionary<string, object?>();

            for (var j = 0; j < headers.Count; j++)
            {
                var value = j < cells.Count ? cells[j]?.ToString()?.Trim() : null;
                dict[headers[j]] = value;
            }

            try
            {
                result.Add(SheetRowFactory.Create(config.SheetType!, dict));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Skipping row {Row} — mapping failed",
                    i);
            }
        }

        return result;
    }

    public void Dispose() => _client.Dispose();
}
