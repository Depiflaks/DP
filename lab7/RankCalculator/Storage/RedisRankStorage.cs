using System.Globalization;
using Sharding;

namespace RankCalculator.Storage;

public class RedisRankStorage(RedisShardStore shards) : IRankStorage
{
    public async Task<string?> GetTextAsync(string textId)
    {
        var database = await shards.FindAsync(textId);
        if (database is null)
        {
            return null;
        }

        var value = await database.StringGetAsync($"TEXT-{textId}");
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SaveRankAsync(string textId, double rank)
    {
        var database = await shards.FindAsync(textId)
            ?? throw new KeyNotFoundException($"Text not found: {textId}");
        await database.StringSetAsync(
            $"RANK-{textId}",
            rank.ToString(CultureInfo.InvariantCulture)
        );
    }
}
