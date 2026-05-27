namespace Valuator.Services;

public interface ITextAnalyzer
{
    double CalculateRank(string text);
    double CalculateSimilarity(string newText, IEnumerable<string> existingTexts);
}