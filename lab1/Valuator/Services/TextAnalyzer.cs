namespace Valuator.Services;

public class TextAnalyzer : ITextAnalyzer
{
    public double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        return text.Distinct().Count();
    }

    public double CalculateSimilarity(string newText, IEnumerable<string> existingTexts)
    {
        if (string.IsNullOrEmpty(newText) || !existingTexts.Any())
            return 0;

        var newWords = newText.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        double maxSimilarity = 0;
        foreach (var existing in existingTexts)
        {
            var existingWords = existing.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.ToLowerInvariant())
                .ToHashSet();

            if (!newWords.Any() && !existingWords.Any())
            {
                maxSimilarity = Math.Max(maxSimilarity, 1.0);
                continue;
            }
            if (!newWords.Any() || !existingWords.Any())
                continue;

            var intersection = newWords.Intersect(existingWords).Count();
            var union = newWords.Union(existingWords).Count();
            var jaccard = (double)intersection / union;
            maxSimilarity = Math.Max(maxSimilarity, jaccard);
        }
        return maxSimilarity;
    }
}