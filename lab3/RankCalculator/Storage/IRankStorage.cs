namespace RankCalculator.Storage;

public interface IRankStorage
{
    Task<string?> GetTextAsync(string textId);
    Task SaveRankAsync(string textId, double rank);
}