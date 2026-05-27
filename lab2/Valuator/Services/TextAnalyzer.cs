namespace Valuator.Services;

public class TextAnalyzer : ITextAnalyzer
{
    public double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var nonAlphabetCount = 0;
        int totalCount = text.Length;
        foreach (char c in text) 
        {
            if (!char.IsLetter(c))
            {
                nonAlphabetCount++;
            }
        }
        return (double)nonAlphabetCount / totalCount;
    }

    public double CalculateSimilarity(string newText, IEnumerable<string> existingTexts)
    {
        if (string.IsNullOrEmpty(newText) || !existingTexts.Any())
            return 0;

        var currSimilarity = 0;

        foreach (var existing in existingTexts)
        {
            if (string.Equals(existing, newText, StringComparison.OrdinalIgnoreCase)) 
            {
                currSimilarity = 1;
            }
        }
        return currSimilarity;
    }
}