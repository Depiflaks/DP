using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sharding;
using Valuator.Messaging;
using Valuator.Services;

namespace Valuator.Pages;

[Authorize]
public class IndexModel : PageModel
{
    [BindProperty]
    public string Country { get; set; } = string.Empty;

    public IEnumerable<string> CountryOptions => Countries.Regions.Keys;

    private readonly ILogger<IndexModel> _logger;
    private readonly IStorageService _storage;
    private readonly ITextAnalyzer _analyzer;
    private readonly IMessagePublisher _messagePublisher;

    public IndexModel(
        ILogger<IndexModel> logger,
        IStorageService storage,
        ITextAnalyzer analyzer,
        IMessagePublisher messagePublisher
    )
    {
        _logger = logger;
        _storage = storage;
        _analyzer = analyzer;
        _messagePublisher = messagePublisher;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        if (string.IsNullOrEmpty(Country) || !Countries.Regions.ContainsKey(Country))
        {
            ModelState.AddModelError(nameof(Country), "Выберите страну гражданства.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            ModelState.AddModelError(string.Empty, "Введите текст для проверки.");
            return Page();
        }

        return await SubmitTextAsync(text);
    }

    private async Task<IActionResult> SubmitTextAsync(string text)
    {
        var id = Guid.NewGuid().ToString();
        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _logger.LogDebug("Text {TextId} submitted by {AuthorId}", id, authorId);
        var allTexts = _storage.GetAllTexts();
        var similarity = _analyzer.CalculateSimilarity(text, allTexts);

        _storage.SaveText(id, text, Country, authorId);
        _storage.SaveSimilarity(id, similarity);

        await _messagePublisher.PublishSimilarityCalculatedAsync(id, similarity);
        await _messagePublisher.PublishRankCalculationAsync(id);

        return Redirect($"summary?id={id}");
    }
}
