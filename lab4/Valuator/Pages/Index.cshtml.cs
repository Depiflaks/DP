using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Messaging;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStorageService _storage;
    private readonly ITextAnalyzer _analyzer;
    private readonly IRankCalculationPublisher _rankCalculationPublisher;

    public IndexModel(
        ILogger<IndexModel> logger,
        IStorageService storage,
        ITextAnalyzer analyzer,
        IRankCalculationPublisher rankCalculationPublisher
    )
    {
        _logger = logger;
        _storage = storage;
        _analyzer = analyzer;
        _rankCalculationPublisher = rankCalculationPublisher;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        _logger.LogDebug(text);
        string id = Guid.NewGuid().ToString();
        if (!string.IsNullOrEmpty(text))
        {
            var allTexts = _storage.GetAllTexts();
            var similarity = _analyzer.CalculateSimilarity(text, allTexts);

            _storage.SaveText(id, text);
            _storage.SaveSimilarity(id, similarity);

            await _rankCalculationPublisher.PublishAsync(id);
        }

        return Redirect($"summary?id={id}");
    }
}