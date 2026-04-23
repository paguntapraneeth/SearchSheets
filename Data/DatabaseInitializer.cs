using Dapper;

namespace SheetsSearchApp.Data;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbConnectionFactory factory,
        ILogger<DatabaseInitializer> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public void Initialize()
    {
        using var conn = _factory.CreateConnection();

        // SQLite performance & safety settings
        conn.Execute("PRAGMA journal_mode=WAL;");
        conn.Execute("PRAGMA synchronous=NORMAL;");
        conn.Execute("PRAGMA foreign_keys=ON;");

        // MAIN DATA TABLE (NO FTS — structured design)
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,

                sheet_id     TEXT,
                sheet_name   TEXT,
                sheet_type   TEXT,

                row_index    INTEGER,
                row_data     TEXT,     -- JSON payload

                content_text TEXT,     -- for optional keyword search
                phone        TEXT      -- 🔥 critical for exact search
            );
        """);

        // INDEX for fast phone lookup (very important)
        conn.Execute("""
            CREATE INDEX IF NOT EXISTS idx_records_phone
            ON records(phone);
        """);

        // SYNC LOG TABLE
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS sync_log (
                sheet_id    TEXT PRIMARY KEY,
                sheet_name  TEXT    NOT NULL,
                sheet_type  TEXT    NOT NULL DEFAULT '',
                rows_synced INTEGER NOT NULL DEFAULT 0,
                synced_at   TEXT    NOT NULL
            );
        """);

        _logger.LogInformation("Database schema initialized (structured mode)");
    }
}