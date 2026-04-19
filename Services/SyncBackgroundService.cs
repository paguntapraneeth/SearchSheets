using Microsoft.Extensions.Options;
using SheetsSearchApp.Models;

namespace SheetsSearchApp.Services;

/// <summary>
/// Runs on a <see cref="PeriodicTimer"/> and syncs every configured Google Sheet
/// into SQLite. Can also be triggered on-demand via <see cref="RunSyncAsync"/>.
/// </summary>
public sealed class SyncBackgroundService : BackgroundService
{
    private readonly ISheetsService                 _sheets;
    private readonly ISearchService                 _search;
    private readonly AppSettings                    _settings;
    private readonly ILogger<SyncBackgroundService> _logger;

    /// <summary>
    /// Ensures the scheduled tick and a manual API-triggered sync
    /// never run concurrently. Capacity = 1 so the second caller waits.
    /// </summary>
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SyncBackgroundService(
        ISheetsService                  sheets,
        ISearchService                  search,
        IOptions<AppSettings>           settings,
        ILogger<SyncBackgroundService>  logger)
    {
        _sheets   = sheets;
        _search   = search;
        _settings = settings.Value;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.Sheets.Count == 0)
        {
            _logger.LogWarning(
                "No sheets configured — background sync is idle. " +
                "Add entries under AppSettings:Sheets in appsettings.json.");
            return;
        }

        if (_settings.SyncOnStartup)
        {
            _logger.LogInformation("Running startup sync…");
            await RunSyncAsync(stoppingToken);
        }

        var interval = TimeSpan.FromMinutes(_settings.SyncIntervalMinutes);
        _logger.LogInformation(
            "Scheduler ready — syncing every {Minutes} minute(s)",
            _settings.SyncIntervalMinutes);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Scheduled sync starting…");
            await RunSyncAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Syncs all configured sheets. Thread-safe — concurrent callers queue
    /// behind the internal semaphore rather than running in parallel.
    /// </summary>
    public async Task<SyncResult> RunSyncAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);

        var result = new SyncResult();

        try
        {
            foreach (var config in _settings.Sheets)
            {
                if (ct.IsCancellationRequested) break;

                var displayName = config.DisplayName
                    ?? config.WorksheetName
                    ?? config.SheetId;

                var sheetType = config.SheetType ?? string.Empty;

                try
                {
                    _logger.LogInformation(
                        "Syncing '{Name}' [{Type}] ({Id})…",
                        displayName, sheetType, config.SheetId);

                    var rows = await _sheets.GetRowsAsync(config, ct);

                    await _search.UpsertSheetRowsAsync(
                        config.SheetId, displayName, sheetType, rows, ct);

                    result.SyncedSheets++;
                    result.TotalRows += rows.Count;

                    _logger.LogInformation(
                        "✓ '{Name}' — {Count} rows synced",
                        displayName, rows.Count);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(
                        "Sync cancelled while processing '{Name}'", displayName);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "✗ Failed to sync '{Name}' ({Id})",
                        displayName, config.SheetId);

                    result.Errors.Add(new SyncError(config.SheetId, ex.Message));
                }
            }

            _logger.LogInformation(
                "Sync complete — {Sheets} sheet(s), {Rows} rows, {Errors} error(s)",
                result.SyncedSheets, result.TotalRows, result.Errors.Count);
        }
        finally
        {
            _lock.Release();
        }

        return result;
    }

    public override void Dispose()
    {
        _lock.Dispose();
        base.Dispose();
    }
}
