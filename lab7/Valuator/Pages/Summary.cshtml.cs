using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;

namespace Valuator.Pages;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IStorageService _storage;

    public SummaryModel(ILogger<SummaryModel> logger, IStorageService storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public double? Rank { get; set; }
    public string TextId { get; private set; } = string.Empty;
    public double Similarity { get; set; }
    public bool IsRankCalculationCompleted { get; set; }

    public IActionResult OnGet(string? id)
    {
        if (!Guid.TryParse(id, out var textId))
        {
            return NotFound();
        }

        id = textId.ToString();
        if (!_storage.IsAuthor(id, User.FindFirstValue(ClaimTypes.NameIdentifier)))
        {
            return NotFound();
        }

        TextId = id;
        _logger.LogDebug("Requested summary for text {TextId}", id);

        IsRankCalculationCompleted = _storage.HasRank(id);
        if (IsRankCalculationCompleted)
        {
            Rank = _storage.GetRank(id);
        }

        Similarity = _storage.GetSimilarity(id);
        return Page();
    }
}
