using StackExchange.Redis;

namespace Valuator.Services;

public class RedisStorageService : IStorageService
{
    private readonly IDatabase _redisDb;
    private readonly IServer _server;
    
    public RedisStorageService(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
    }
    
    public void SaveText(string id, string text)
    {
        _redisDb.StringSet($"TEXT-{id}", text);
    }
    
    public void SaveRank(string id, double rank)
    {
        _redisDb.StringSet($"RANK-{id}", rank.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    
    public void SaveSimilarity(string id, double similarity)
    {
        _redisDb.StringSet($"SIMILARITY-{id}", similarity.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    
    public string? GetText(string id)
    {
        return _redisDb.StringGet($"TEXT-{id}");
    }
    
    public double GetRank(string id)
    {
        var value = _redisDb.StringGet($"RANK-{id}");
        return value.HasValue ? double.Parse(value!, System.Globalization.CultureInfo.InvariantCulture) : 0;
    }
    
    public double GetSimilarity(string id)
    {
        var value = _redisDb.StringGet($"SIMILARITY-{id}");
        return value.HasValue ? double.Parse(value!, System.Globalization.CultureInfo.InvariantCulture) : 0;
    }
    
    public IEnumerable<string> GetAllTexts()
    {
        var keys = _server.Keys(pattern: "TEXT-*");
        foreach (var key in keys)
        {
            var value = _redisDb.StringGet(key);
            if (value.HasValue)
                yield return value!;
        }
    }
}