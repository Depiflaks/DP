namespace Valuator.Services;

public class TextAnalyzer : ITextAnalyzer
{
    public double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return (double)text.Count(character => !char.IsLetter(character)) / text.Length;
    }

    public double CalculateSimilarity(string newText, IEnumerable<string> existingTexts)
    {
        if (string.IsNullOrEmpty(newText))
        {
            return 0;
        }

        return existingTexts.Any(existing => string.Equals(existing, newText, StringComparison.OrdinalIgnoreCase))
            ? 1
            : 0;
    }
}
