using Dapper;

namespace SheetsSearchApp.Data;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory        _factory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbConnectionFactory         factory,
        ILogger<DatabaseInitializer> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    public void Initialize()
    {
        using var conn = _factory.CreateConnection();

        conn.Execute("PRAGMA journal_mode=WAL;");
        conn.Execute("PRAGMA synchronous=NORMAL;");
        conn.Execute("PRAGMA foreign_keys=ON;");

        // FTS5 virtual table.
        // sheet_type is stored (UNINDEXED) so controllers can filter by type
        // without going through FTS. row_data stores the typed model as JSON.
        conn.Execute("""
            CREATE VIRTUAL TABLE IF NOT EXISTS records USING fts5(
                sheet_id     UNINDEXED,
                sheet_name   UNINDEXED,
                sheet_type   UNINDEXED,
                row_index    UNINDEXED,
                row_data     UNINDEXED,
                content_text,
                tokenize = 'porter ascii'
            )
            """);

        // Sync audit — one row per sheet, updated after each successful sync.
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS sync_log (
                sheet_id    TEXT PRIMARY KEY,
                sheet_name  TEXT    NOT NULL,
                sheet_type  TEXT    NOT NULL DEFAULT '',
                rows_synced INTEGER NOT NULL DEFAULT 0,
                synced_at   TEXT    NOT NULL
            )
            """);

        _logger.LogInformation("Database schema ready");
    }
}
