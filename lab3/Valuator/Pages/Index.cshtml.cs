using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStorageService _storage;
    private readonly ITextAnalyzer _analyzer;

    public IndexModel(
        ILogger<IndexModel> logger,
        IStorageService storage,
        ITextAnalyzer analyzer
    )
    {
        _logger = logger;
        _storage = storage;
        _analyzer = analyzer;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);
        string id = Guid.NewGuid().ToString();
        if (!string.IsNullOrEmpty(text)) 
        {
            double rank = _analyzer.CalculateRank(text);
            var allTexts = _storage.GetAllTexts();
            double similarity = _analyzer.CalculateSimilarity(text, allTexts);
            
            _storage.SaveText(id, text);
            _storage.SaveRank(id, rank);
            _storage.SaveSimilarity(id, similarity);
        }

        return Redirect($"summary?id={id}");
    }
}