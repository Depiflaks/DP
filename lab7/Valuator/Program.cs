using Microsoft.AspNetCore.DataProtection;
using Sharding;
using Valuator.Messaging;
using Valuator.Services;
using Valuator.Hubs;
using Valuator.Authentication;

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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapHub<SummaryHub>("/hubs/summary", options => options.CloseOnAuthenticationExpiration = true);

        app.Run();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        var rabbitMqOptions = RabbitMqOptions.FromEnvironment();

        var keysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "keys");
        services.AddDataProtection().SetApplicationName("Valuator")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        services.AddSingleton<RedisShardStore>();
        services.AddSingleton(rabbitMqOptions);
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        services.AddScoped<IStorageService, RedisStorageService>();
        services.AddScoped<ITextAnalyzer, TextAnalyzer>();
        services.AddUserAuthentication();

        services.AddRazorPages();
        services.AddSignalR();
        services.AddHostedService<RankCalculatedSubscriber>();
    }
}
