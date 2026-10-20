using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Sharding;

public sealed class RedisShardStore : IDisposable
{
    private readonly ConnectionMultiplexer _main;
    private readonly Dictionary<string, ConnectionMultiplexer> _shards;
    private readonly ILogger<RedisShardStore> _logger;

    public RedisShardStore(ILogger<RedisShardStore> logger)
    {
        _logger = logger;
        _main = Connect("MAIN", 6000);
        _shards = new Dictionary<string, ConnectionMultiplexer>
        {
            ["RU"] = Connect("RU", 6001),
            ["EU"] = Connect("EU", 6002),
            ["ASIA"] = Connect("ASIA", 6003)
        };
    }

    private static ConnectionMultiplexer Connect(string region, int port)
    {
        var endpoint = Environment.GetEnvironmentVariable($"DB_{region}") ?? $"localhost:{port}";
        return ConnectionMultiplexer.Connect($"{endpoint},abortConnect=false");
    }

    public void SaveText(string id, string text, string country)
    {
        if (!Countries.Regions.TryGetValue(country, out var region))
        {
            throw new ArgumentException("Unknown country.", nameof(country));
        }

        var database = _shards[region].GetDatabase();
        database.StringSet(new KeyValuePair<RedisKey, RedisValue>[]
        {
            new($"TEXT-{id}", text),
            new($"COUNTRY-{id}", country)
        });
        _main.GetDatabase().StringSet($"SHARD-{id}", region);
    }

    public IDatabase? Find(string id)
    {
        var region = _main.GetDatabase().StringGet($"SHARD-{id}");
        return Resolve(id, region);
    }

    public async Task<IDatabase?> FindAsync(string id)
    {
        var region = await _main.GetDatabase().StringGetAsync($"SHARD-{id}");
        return Resolve(id, region);
    }

    private IDatabase? Resolve(string id, RedisValue region)
    {
        _logger.LogInformation("LOOKUP: {TextId}, {ShardKey}", id, region.HasValue ? region.ToString() : "UNKNOWN");
        if (!region.HasValue)
        {
            return null;
        }

        if (!_shards.TryGetValue(region.ToString(), out var shard))
        {
            throw new InvalidOperationException($"Unknown shard: {region}");
        }

        return shard.GetDatabase();
    }

    public IEnumerable<string> GetTextIds()
    {
        var server = _main.GetServer(_main.GetEndPoints().First());
        return server.Keys(pattern: "SHARD-*").Select(key => key.ToString()[6..]);
    }

    public void Dispose()
    {
        _main.Dispose();
        foreach (var shard in _shards.Values)
        {
            shard.Dispose();
        }
    }
}
