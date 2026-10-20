namespace RankCalculator.Services;

public class TextRankCalculator : ITextRankCalculator
{
    public double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return (double)text.Count(character => !char.IsLetter(character)) / text.Length;
    }
}
