using System.Globalization;
using Sharding;

namespace Valuator.Services;

public class RedisStorageService(RedisShardStore shards) : IStorageService
{
    public void SaveText(string id, string text, string country, string authorId)
    {
        shards.SaveText(id, text, country, authorId);
    }

    public bool IsAuthor(string id, string? authorId)
    {
        if (string.IsNullOrEmpty(authorId))
        {
            return false;
        }

        var author = shards.Find(id)?.StringGet($"AUTHOR-{id}");
        return author.HasValue && author.Value.HasValue && author.Value.ToString() == authorId;
    }

    public void SaveRank(string id, double rank)
    {
        SaveValue(id, "RANK", rank);
    }

    public void SaveSimilarity(string id, double similarity)
    {
        SaveValue(id, "SIMILARITY", similarity);
    }

    private void SaveValue(string id, string prefix, double value)
    {
        var database = shards.Find(id) ?? throw new KeyNotFoundException($"Text not found: {id}");
        database.StringSet($"{prefix}-{id}", value.ToString(CultureInfo.InvariantCulture));
    }

    public string? GetText(string id)
    {
        var database = shards.Find(id);
        if (database is null)
        {
            return null;
        }

        var value = database.StringGet($"TEXT-{id}");
        return value.HasValue ? value.ToString() : null;
    }

    public double GetRank(string id)
    {
        return GetValue(id, "RANK");
    }

    public double GetSimilarity(string id)
    {
        return GetValue(id, "SIMILARITY");
    }

    private double GetValue(string id, string prefix)
    {
        var value = shards.Find(id)?.StringGet($"{prefix}-{id}");
        return value.HasValue && value.Value.HasValue
            ? double.Parse(value.Value.ToString(), CultureInfo.InvariantCulture)
            : 0;
    }

    public IEnumerable<string> GetAllTexts()
    {
        foreach (var id in shards.GetTextIds())
        {
            var text = GetText(id);
            if (text is not null)
            {
                yield return text;
            }
        }
    }

    public bool HasRank(string id)
    {
        return shards.Find(id)?.KeyExists($"RANK-{id}") ?? false;
    }
}
