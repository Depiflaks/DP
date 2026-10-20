using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Sharding;
using StackExchange.Redis;

namespace Valuator.Authentication;

public sealed class UserStore(RedisShardStore shards, IPasswordHasher<UserAccount> hasher)
{
    public async Task<UserAccount?> RegisterAsync(string login, string password)
    {
        var user = new UserAccount(Guid.NewGuid().ToString(), login, string.Empty);
        user = user with { PasswordHash = hasher.HashPassword(user, password) };
        var created = await shards.MainDatabase.StringSetAsync(Key(login),
            JsonSerializer.Serialize(user), when: When.NotExists);
        return created ? user : null;
    }

    public async Task<UserAccount?> AuthenticateAsync(string login, string password)
    {
        var value = await shards.MainDatabase.StringGetAsync(Key(login));
        if (!value.HasValue)
        {
            return null;
        }

        var user = JsonSerializer.Deserialize<UserAccount>(value.ToString())!;
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    private static string Key(string login)
    {
        return $"USER-{login.ToUpperInvariant()}";
    }
}
