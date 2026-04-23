using Serilog;
using Serilog.Events;
using SheetsSearchApp.Data;
using SheetsSearchApp.Models;
using SheetsSearchApp.Services;

// ── Bootstrap logger (used until the host is built) ─────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    // ── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .MinimumLevel.Override("Microsoft.AspNetCore",       LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path:                   "logs/app-.log",
            rollingInterval:        RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    // ── Configuration ────────────────────────────────────────────────────────
    builder.Services.Configure<AppSettings>(
        builder.Configuration.GetSection("AppSettings"));

    // ── Data layer ───────────────────────────────────────────────────────────
    builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
    builder.Services.AddSingleton<DatabaseInitializer>();

    // ── Application services ─────────────────────────────────────────────────
    builder.Services.AddSingleton<ISearchService, SearchService>();
    builder.Services.AddSingleton<ISheetsService, SheetsService>();

    // SyncBackgroundService is registered as both a singleton and a hosted service.
    // Singleton registration lets SyncController inject and call RunSyncAsync directly.
    // AddHostedService resolves the same singleton so only one instance ever runs.
    builder.Services.AddSingleton<SyncBackgroundService>();
    builder.Services.AddHostedService(sp =>
        sp.GetRequiredService<SyncBackgroundService>());

    // ── API ──────────────────────────────────────────────────────────────────
    builder.Services
        .AddControllers()
        .AddJsonOptions(opts =>
        {
            // Serialise enums as strings (e.g. "Active" not 1).
            opts.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
            // Omit null fields from API responses.
            opts.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddProblemDetails();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title       = "Sheets Search API",
            Version     = "v1",
            Description = "Full-text search over synced Google Sheets data (Products & Customers)"
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            c.IncludeXmlComments(xmlPath);
    });

    // ── CORS (Vite dev server) ────────────────────────────────────────────────
    builder.Services.AddCors(opt => opt.AddPolicy("DevCors", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()));

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Initialise DB schema before anything starts ───────────────────────────
    app.Services.GetRequiredService<DatabaseInitializer>().Initialize();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseExceptionHandler();   // returns RFC 7807 ProblemDetails on unhandled exceptions

    app.UseSerilogRequestLogging(opt =>
    {
        opt.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0}ms";
        opt.GetLevel = (ctx, _, _) =>
            ctx.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseCors("DevCors");
        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sheets Search v1"));
    }

    app.UseDefaultFiles();   // serves wwwroot/index.html for GET /
    app.UseStaticFiles();

    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapFallbackToFile("index.html");   // SPA fallback

    Log.Information("SheetsSearchApp starting…");
    app.Run();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
