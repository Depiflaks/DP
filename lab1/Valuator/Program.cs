using StackExchange.Redis;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        RegisterServices(builder.Services);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Program");
        logger.LogInformation("🔍 VALUE OF REDIS_HOST IS: '{RedisHost}'", redisHost);
        var redis = ConnectionMultiplexer.Connect($"{redisHost}:6379");
        services.AddSingleton<IConnectionMultiplexer>(redis);

        services.AddScoped<IStorageService, RedisStorageService>();
        services.AddScoped<ITextAnalyzer, TextAnalyzer>();

        services.AddRazorPages();
    }
}
