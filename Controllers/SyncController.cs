using Microsoft.AspNetCore.Mvc;
using SheetsSearchApp.Models;
using SheetsSearchApp.Services;

namespace SheetsSearchApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class SyncController : ControllerBase
{
    private readonly SyncBackgroundService   _syncService;
    private readonly ISearchService          _searchService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        SyncBackgroundService   syncService,
        ISearchService          searchService,
        ILogger<SyncController> logger)
    {
        _syncService   = syncService;
        _searchService = searchService;
        _logger        = logger;
    }

    /// <summary>
    /// Manually triggers a full sync of all configured sheets.
    /// Returns 200 if all sheets synced successfully,
    /// 207 if some sheets succeeded and some failed.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SyncResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<SyncResult>(StatusCodes.Status207MultiStatus)]
    public async Task<IActionResult> TriggerSync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Manual sync triggered via API from {IP}",
            HttpContext.Connection.RemoteIpAddress);

        var result = await _syncService.RunSyncAsync(ct);

        // 207 Multi-Status signals partial success to the caller.
        return result.Success
            ? Ok(result)
            : StatusCode(StatusCodes.Status207MultiStatus, result);
    }
}
