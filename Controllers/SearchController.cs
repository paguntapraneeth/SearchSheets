using Microsoft.AspNetCore.Mvc;
using SheetsSearchApp.Models;
using SheetsSearchApp.Services;

namespace SheetsSearchApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService            _search;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService            search,
        ILogger<SearchController> logger)
    {
        _search = search;
        _logger = logger;
    }

    /// <summary>
    /// Full-text search across all synced sheets, or a specific sheet type.
    /// Results include the typed row data (ProductRow or CustomerRow).
    /// </summary>
    /// <param name="q">Search terms (min 2 characters).</param>
    /// <param name="limit">Max results to return (1–100, default 50).</param>
    /// <param name="sheetName">Optional: restrict results to a specific sheet name.</param>
    [HttpGet]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int     limit     = 50,
        [FromQuery] string? sheetName = null,
        CancellationToken   ct        = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid query",
                Detail = "Search query must be at least 2 characters.",
                Status = StatusCodes.Status400BadRequest
            });

        if (limit is < 1 or > 100)
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid limit",
                Detail = "Limit must be between 1 and 100.",
                Status = StatusCodes.Status400BadRequest
            });

        _logger.LogDebug(
            "Search: q='{Query}' limit={Limit} sheetName='{Sheet}'",
            q, limit, sheetName ?? "all");

        var request = new SearchRequest
        {
            Query     = q.Trim(),
            Limit     = limit,
            SheetName = sheetName?.Trim()
        };

        var result = await _search.SearchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Search limited to Product sheet rows only.
    /// </summary>
    [HttpGet("products")]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] int     limit = 50,
        CancellationToken   ct    = default)
        => Search(q, limit, sheetName: "Products", ct);

    /// <summary>
    /// Search limited to Customer sheet rows only.
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> SearchCustomers(
        [FromQuery] string? q,
        [FromQuery] int     limit = 50,
        CancellationToken   ct    = default)
        => Search(q, limit, sheetName: "Customers", ct);
}
