using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Services;

namespace Dashboard.Controllers;

[Authorize]
public class GlobalSearchController : Controller
{
    private readonly GlobalSearchService _globalSearch;

    public GlobalSearchController(GlobalSearchService globalSearch)
    {
        _globalSearch = globalSearch;
    }

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var result = await _globalSearch.SearchAsync(User, q, HttpContext, cancellationToken);
        return Json(result);
    }
}
