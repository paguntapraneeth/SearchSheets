# SheetsSearchApp

ASP.NET Core 8 web application that syncs Google Sheets into a local SQLite
full-text search index and exposes a clean search API + UI.

## Project Structure

```
SheetsSearchApp/
│
├── Controllers/
│   ├── SearchController.cs        # GET /api/search, /api/search/products, /api/search/customers
│   └── SyncController.cs          # POST /api/sync, GET /api/sync/status
│
├── Data/
│   ├── IDbConnectionFactory.cs    # Abstraction for connection creation
│   ├── SqliteConnectionFactory.cs # SQLite implementation (WAL mode)
│   └── DatabaseInitializer.cs     # FTS5 schema bootstrap on startup
│
├── Models/
│   ├── AppSettings.cs             # Config binding (CredentialsFilePath, Sheets, etc.)
│   ├── SheetConfig.cs             # Per-sheet config (SheetId, SheetType, DisplayName)
│   ├── ISheetRow.cs               # Interface all sheet models implement
│   ├── ProductRow.cs              # Typed model for Products sheet
│   ├── CustomerRow.cs             # Typed model for Customers sheet
│   ├── SheetRowFactory.cs         # Maps SheetType string → correct model
│   ├── DictionaryExtensions.cs    # GetString / GetDecimal / GetBool helpers
│   ├── SearchDtos.cs              # SearchRequest, SearchResponse, SearchItem
│   └── SyncDtos.cs                # SyncResult, SyncError, SyncStatusResponse
│
├── Services/
│   ├── ISearchService.cs          # Search + upsert + status contract
│   ├── SearchService.cs           # FTS5 upsert and search implementation (Dapper)
│   ├── ISheetsService.cs          # Google Sheets fetch contract
│   ├── SheetsService.cs           # Google Sheets API v4 client
│   └── SyncBackgroundService.cs   # PeriodicTimer + SemaphoreSlim sync
│
├── wwwroot/
│   └── index.html                 # Search UI (vanilla HTML/JS, no build step)
│
├── Program.cs                     # DI, Serilog, Swagger, middleware pipeline
├── appsettings.json
├── appsettings.Production.json
└── SheetsSearchApp.csproj
```

---

## Adding a New Sheet Type

1. Create `Models/OrderRow.cs` implementing `ISheetRow`:
   ```csharp
   public sealed class OrderRow : ISheetRow
   {
       public string OrderId  { get; init; } = string.Empty;
       public string Customer { get; init; } = string.Empty;
       // ...

       public static OrderRow FromRow(IDictionary<string, object?> row) => new()
       {
           OrderId  = row.GetString("Order ID"),
           Customer = row.GetString("Customer"),
       };

       public string ToSearchText() => string.Join(" ", OrderId, Customer);
   }
   ```

2. Add a case in `Models/SheetRowFactory.cs`:
   ```csharp
   "order" => OrderRow.FromRow(rawRow),
   ```

3. Add `"SheetType": "Order"` to the sheet entry in `appsettings.json`.

4. Optionally add a typed search endpoint in `SearchController.cs`.

That's it — no other changes needed.

---

## Google Cloud Setup

1. [Google Cloud Console](https://console.cloud.google.com) → Enable **Google Sheets API**
2. IAM → Service Accounts → Create → Keys → Add Key → JSON → download
3. Save as `credentials.json` beside the `.csproj`
4. Share each spreadsheet with the service account email (Viewer role)

---

## Configuration (`appsettings.json`)

```json
{
  "AppSettings": {
    "CredentialsFilePath":  "credentials.json",
    "SyncIntervalMinutes":  60,
    "SyncOnStartup":        true,
    "MaxSearchResults":     100,
    "Sheets": [
      {
        "SheetId":       "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgVE2upms",
        "WorksheetName": "",
        "SheetType":     "Product",
        "DisplayName":   "Products"
      },
      {
        "SheetId":       "ANOTHER_SHEET_ID",
        "WorksheetName": "Sheet1",
        "SheetType":     "Customer",
        "DisplayName":   "Customers"
      }
    ]
  }
}
```

Sheet ID is in the URL:
`https://docs.google.com/spreadsheets/d/**THIS_PART**/edit`

---

## Running Locally

```bash
dotnet restore
dotnet run
# Swagger UI → https://localhost:5001/swagger
# Search UI  → https://localhost:5001
```

---

## API Reference

| Method | Path                       | Description                             |
|--------|----------------------------|-----------------------------------------|
| GET    | `/api/search?q=`           | Search all sheets                       |
| GET    | `/api/search?q=&sheetName=`| Search filtered to one sheet            |
| GET    | `/api/search/products?q=`  | Search Products only                    |
| GET    | `/api/search/customers?q=` | Search Customers only                   |
| POST   | `/api/sync`                | Manual sync (200 = ok, 207 = partial)   |
| GET    | `/api/sync/status`         | Row counts + last sync time per sheet   |
| GET    | `/health`                  | Health check                            |
| GET    | `/swagger`                 | Swagger UI (dev only)                   |

---

## Deployment

### Hetzner VPS — $4/mo (recommended)

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish
scp -r ./publish user@server:/opt/sheetssearch
scp credentials.json user@server:/opt/sheetssearch/
```

`/etc/systemd/system/sheetssearch.service`:
```ini
[Unit]
Description=SheetsSearchApp
After=network.target

[Service]
WorkingDirectory=/opt/sheetssearch
ExecStart=/opt/sheetssearch/SheetsSearchApp
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
User=www-data

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload && sudo systemctl enable --now sheetssearch
```

### Docker (Railway / Fly.io)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SheetsSearchApp.dll"]
```

Mount a persistent volume at `/app/data` and set:
```
ConnectionStrings__Sqlite=Data Source=/app/data/data.db;Cache=Shared;
```
