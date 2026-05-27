namespace RankCalculator.Services;

public class TextRankCalculator : ITextRankCalculator
{
    public double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var nonAlphabetCount = 0;
        var totalCount = text.Length;

        foreach (var c in text)
        {
            if (!char.IsLetter(c))
            {
                nonAlphabetCount++;
            }
        }

        return (double)nonAlphabetCount / totalCount;
    }
}