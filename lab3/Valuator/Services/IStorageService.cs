namespace Valuator.Services;

public interface IStorageService
{
    void SaveText(string id, string text);
    void SaveRank(string id, double rank);
    void SaveSimilarity(string id, double similarity);
    string? GetText(string id);
    double GetRank(string id);
    double GetSimilarity(string id);
    bool HasRank(string id);
    IEnumerable<string> GetAllTexts();
}
