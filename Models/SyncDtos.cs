namespace SheetsSearchApp.Models;

// ─── Sync result ───────────────────────────────────────────────────────────

public sealed class SyncResult
{
    public int             SyncedSheets { get; set; }
    public int             TotalRows    { get; set; }
    public List<SyncError> Errors       { get; set; } = [];
    public bool            Success      => Errors.Count == 0;
}

public sealed record SyncError(string SheetId, string Message);

// ─── Status response ───────────────────────────────────────────────────────

public sealed class SyncStatusResponse
{
    public int                       TotalRows { get; init; }
    public IReadOnlyList<SheetSyncInfo> Sheets { get; init; } = [];
}

public sealed class SheetSyncInfo
{
    public string         SheetId     { get; init; } = string.Empty;
    public string         SheetName   { get; init; } = string.Empty;
    public string         SheetType   { get; init; } = string.Empty;
    public int            RowsSynced  { get; init; }
    public DateTimeOffset SyncedAt    { get; init; }
}
