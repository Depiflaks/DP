using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Messaging;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
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
        _logger.LogDebug("Submitted text: {Text}", text);
        var id = Guid.NewGuid().ToString();
        if (string.IsNullOrEmpty(text))
        {
            return Redirect($"summary?id={id}");
        }

        var allTexts = _storage.GetAllTexts();
        var similarity = _analyzer.CalculateSimilarity(text, allTexts);

        _storage.SaveText(id, text);
        _storage.SaveSimilarity(id, similarity);

        await _messagePublisher.PublishSimilarityCalculatedAsync(id, similarity);
        await _messagePublisher.PublishRankCalculationAsync(id);

        return Redirect($"summary?id={id}");
    }
}
