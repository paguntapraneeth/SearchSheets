namespace SheetsSearchApp.Models;

public sealed class AppSettings
{
    public string           CredentialsFilePath { get; init; } = "credentials.json";
    public List<SheetConfig> Sheets             { get; init; } = [];
    public int              SyncIntervalMinutes { get; init; } = 60;
    public bool             SyncOnStartup       { get; init; } = true;
    public int              MaxSearchResults    { get; init; } = 100;
}
