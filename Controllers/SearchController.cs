using Microsoft.AspNetCore.Mvc;
using SheetsSearchApp.Services;

namespace SheetsSearchApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search by phone number (triggered manually from UI)
    /// </summary>
    [HttpGet("phone")]
    public async Task<IActionResult> SearchByPhone(
        [FromQuery] string phone,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("Phone is required");

        _logger.LogInformation("Phone search triggered: {Phone}", phone);

        var results = await _searchService.SearchByPhoneAsync(phone.Trim(), ct);

        return Ok(results);
    }
}