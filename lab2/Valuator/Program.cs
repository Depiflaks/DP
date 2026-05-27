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

        app.MapGet("/instance", () => new
        {
            Instance = Environment.MachineName
        });

        app.Run();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        var redis = ConnectionMultiplexer.Connect($"{redisHost}:6379,abortConnect=false");

        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddScoped<IStorageService, RedisStorageService>();
        services.AddScoped<ITextAnalyzer, TextAnalyzer>();

        services.AddRazorPages();
    }
}
