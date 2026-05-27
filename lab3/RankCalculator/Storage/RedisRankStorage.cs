using System.Globalization;
using StackExchange.Redis;

namespace RankCalculator.Storage;

public class RedisRankStorage : IRankStorage
{
    private readonly IDatabase _database;

    public RedisRankStorage(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<string?> GetTextAsync(string textId)
    {
        var value = await _database.StringGetAsync($"TEXT-{textId}");

        if (value.IsNullOrEmpty)
            return null;

        return value.ToString();
    }

    public Task SaveRankAsync(string textId, double rank)
    {
        return _database.StringSetAsync(
            $"RANK-{textId}",
            rank.ToString(CultureInfo.InvariantCulture)
        );
    }
}