using Dapper;
using Microsoft.Extensions.Options;
using SheetsSearchApp.Data;
using SheetsSearchApp.Models;
using System.Text.Json;

namespace SheetsSearchApp.Services;

public sealed class SearchService : ISearchService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<SearchService> _logger;

 
    private static readonly JsonSerializerOptions _readOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    
    private static readonly JsonSerializerOptions _writeOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SearchService(
        IDbConnectionFactory db,
        ILogger<SearchService> logger,
        IOptions<AppSettings> settings)
    {
        _db = db;
        _logger = logger;
    }

   
    public async Task<List<SearchItem>> SearchByPhoneAsync(string phone, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT sheet_name, sheet_type, row_index, row_data
            FROM records
            WHERE sheet_type = 'appointment'
              AND phone = @Phone",
            new { Phone = phone.Trim() });

        var results = rows
            .Select(r => BuildSearchItem(r))
            .Where(x => x != null)
            .Cast<SearchItem>()
            .ToList();

        _logger.LogInformation("Phone search '{Phone}' returned {Count} results", phone, results.Count);

        return results;
    }

    private static SearchItem? BuildSearchItem(dynamic r)
    {
        try
        {
            var sheetType = ((string)r.sheet_type).ToLower();
            var rowData = (string)r.row_data;

            if (sheetType != "appointment")
                return null;

            var data = JsonSerializer.Deserialize<AppointmentRow>(rowData, _readOpts);

            if (data == null)
                return null;

            return new SearchItem
            {
                SheetName = (string)r.sheet_name,
                SheetType = sheetType,
                RowIndex = (int)r.row_index,

            
                Name = data.Name,
                Phone = data.ContactNo,
                TH = data.Treatment,
                RO = data.Specialist,
                AM = data.Amount
            };
        }
        catch
        {
            return null;
        }
    }

  
    public async Task UpsertSheetRowsAsync(
        string sheetId,
        string sheetName,
        string sheetType,
        IList<ISheetRow> rows,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // 1️⃣ Remove existing rows for this sheet
        await conn.ExecuteAsync(
            "DELETE FROM records WHERE sheet_name = @SheetName",
            new { SheetName = sheetName });

        // 2️⃣ Insert fresh rows
        var index = 0;

        foreach (var row in rows)
        {
            var content = row.ToSearchText();
            var phone = (row as AppointmentRow)?.ContactNo?.Trim();

            
            var json = row is AppointmentRow a
                ? JsonSerializer.Serialize(a, _writeOpts)
                : "{}";

            await conn.ExecuteAsync(@"
                INSERT INTO records
                (sheet_name, sheet_type, row_index, row_data, content_text, phone)
                VALUES (@SheetName, @SheetType, @RowIndex, @RowData, @ContentText, @Phone)",
                new
                {
                    SheetName = sheetName,
                    SheetType = sheetType,
                    RowIndex = index++,
                    RowData = json, 
                    ContentText = content,
                    Phone = phone
                });
        }
    }
}