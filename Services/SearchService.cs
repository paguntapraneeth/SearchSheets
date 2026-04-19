using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.Extensions.Options;
using SheetsSearchApp.Data;
using SheetsSearchApp.Models;

namespace SheetsSearchApp.Services;

public sealed class SearchService : ISearchService
{
    private readonly IDbConnectionFactory   _db;
    private readonly ILogger<SearchService> _logger;
    private readonly AppSettings            _settings;

    // Polymorphic serialisation: ISheetRow is serialised as the concrete type
    // so ProductRow / CustomerRow fields appear in the JSON response.
    private static readonly JsonSerializerOptions _writeOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        Converters                  = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions _readOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SearchService(
        IDbConnectionFactory   db,
        ILogger<SearchService> logger,
        IOptions<AppSettings>  settings)
    {
        _db       = db;
        _logger   = logger;
        _settings = settings.Value;
    }

    // ── Search ───────────────────────────────────────────────────────────

    public async Task<SearchResponse> SearchAsync(
        SearchRequest request, CancellationToken ct = default)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        if (query.Length < 2)
            return new SearchResponse { Query = query, Count = 0, Results = [] };

        var limit = Math.Clamp(request.Limit, 1, _settings.MaxSearchResults);

        using var conn = _db.CreateConnection();

        IEnumerable<dynamic> rows;

        try
        {
            // Optionally scope search to a single sheet name.
            var sql = string.IsNullOrWhiteSpace(request.SheetName)
                ? """
                  SELECT sheet_name, sheet_type,
                         CAST(row_index AS INTEGER) AS row_index,
                         row_data,
                         snippet(records, 5, '<mark>', '</mark>', '…', 24) AS snippet
                  FROM   records
                  WHERE  records MATCH @Query
                  ORDER  BY rank
                  LIMIT  @Limit
                  """
                : """
                  SELECT sheet_name, sheet_type,
                         CAST(row_index AS INTEGER) AS row_index,
                         row_data,
                         snippet(records, 5, '<mark>', '</mark>', '…', 24) AS snippet
                  FROM   records
                  WHERE  records MATCH @Query
                    AND  sheet_name = @SheetName
                  ORDER  BY rank
                  LIMIT  @Limit
                  """;

            rows = await conn.QueryAsync<dynamic>(new CommandDefinition(
                sql,
                new { Query = EscapeFts(query), Limit = limit, request.SheetName },
                cancellationToken: ct));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "FTS5 query failed for '{Query}'; falling back to LIKE", query);

            rows = await conn.QueryAsync<dynamic>(new CommandDefinition(
                """
                SELECT sheet_name, sheet_type,
                       CAST(row_index AS INTEGER) AS row_index,
                       row_data,
                       content_text AS snippet
                FROM   records
                WHERE  content_text LIKE @Pattern
                LIMIT  @Limit
                """,
                new { Pattern = $"%{query}%", Limit = limit },
                cancellationToken: ct));
        }

        var results = rows
            .Select(r => BuildSearchItem(r))
            .Where(item => item is not null)
            .Cast<SearchItem>()
            .ToList();

        return new SearchResponse
        {
            Query   = query,
            Count   = results.Count,
            Results = results
        };
    }

    private static SearchItem? BuildSearchItem(dynamic r)
    {
        try
        {
            var sheetType = (string)r.sheet_type;
            var rowData   = (string)r.row_data;

            // Deserialise back to the correct typed model.
            ISheetRow data = sheetType.ToLowerInvariant() switch
            {
                "product"  => JsonSerializer.Deserialize<ProductRow>(rowData, _readOpts)!,
                "customer" => JsonSerializer.Deserialize<CustomerRow>(rowData, _readOpts)!,
                _          => throw new NotSupportedException(sheetType)
            };

            return new SearchItem
            {
                SheetName = (string)r.sheet_name,
                SheetType = sheetType,
                RowIndex  = (int)   r.row_index,
                Snippet   = (string)r.snippet,
                Data      = data
            };
        }
        catch (Exception)
        {
            return null;   // skip malformed rows rather than crashing the whole search
        }
    }

    // ── Upsert ───────────────────────────────────────────────────────────

    public async Task UpsertSheetRowsAsync(
        string           sheetId,
        string           sheetName,
        string           sheetType,
        IList<ISheetRow> rows,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var tx   = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(
                "DELETE FROM records WHERE sheet_id = @SheetId",
                new { SheetId = sheetId }, tx);

            for (var i = 0; i < rows.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var row     = rows[i];
                var rowJson = JsonSerializer.Serialize(row, row.GetType(), _writeOpts);

                await conn.ExecuteAsync(
                    """
                    INSERT INTO records
                        (sheet_id, sheet_name, sheet_type, row_index, row_data, content_text)
                    VALUES
                        (@SheetId, @SheetName, @SheetType, @RowIndex, @RowData, @ContentText)
                    """,
                    new
                    {
                        SheetId     = sheetId,
                        SheetName   = sheetName,
                        SheetType   = sheetType,
                        RowIndex    = i,
                        RowData     = rowJson,
                        ContentText = row.ToSearchText()
                    }, tx);
            }

            await conn.ExecuteAsync(
                """
                INSERT OR REPLACE INTO sync_log
                    (sheet_id, sheet_name, sheet_type, rows_synced, synced_at)
                VALUES
                    (@SheetId, @SheetName, @SheetType, @RowsSynced, @SyncedAt)
                """,
                new
                {
                    SheetId    = sheetId,
                    SheetName  = sheetName,
                    SheetType  = sheetType,
                    RowsSynced = rows.Count,
                    SyncedAt   = DateTimeOffset.UtcNow.ToString("o")
                }, tx);

            tx.Commit();
            _logger.LogInformation(
                "Upserted {Count} rows → '{Name}' [{Type}]",
                rows.Count, sheetName, sheetType);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ── Status ───────────────────────────────────────────────────────────

    public async Task<SyncStatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM records",
                cancellationToken: ct));

        var logs = await conn.QueryAsync<dynamic>(
            new CommandDefinition(
                "SELECT * FROM sync_log ORDER BY synced_at DESC",
                cancellationToken: ct));

        var sheets = logs.Select(l => new SheetSyncInfo
        {
            SheetId    = (string)l.sheet_id,
            SheetName  = (string)l.sheet_name,
            SheetType  = (string)l.sheet_type,
            RowsSynced = (int)   l.rows_synced,
            SyncedAt   = DateTimeOffset.Parse((string)l.synced_at)
        }).ToList();

        return new SyncStatusResponse { TotalRows = total, Sheets = sheets };
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string EscapeFts(string query) => query.Replace("\"", "\"\"");
}
