using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using Valuator.Messaging;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        RegisterServices(builder.Services);

        var app = builder.Build();

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
        var redis = ConnectionMultiplexer.Connect($"{redisHost}:6379,abortConnect=false");

        var rabbitMqOptions = RabbitMqOptions.FromEnvironment();

        services.AddDataProtection().PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");

        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton(rabbitMqOptions);
        services.AddSingleton<IRankCalculationPublisher, RabbitMqRankCalculationPublisher>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddScoped<IStorageService, RedisStorageService>();
        services.AddScoped<ITextAnalyzer, TextAnalyzer>();

        services.AddRazorPages();
    }
}
