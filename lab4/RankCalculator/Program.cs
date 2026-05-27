using RankCalculator;
using RankCalculator.Messaging;
using RankCalculator.Services;
using RankCalculator.Storage;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
var redis = ConnectionMultiplexer.Connect($"{redisHost}:6379,abortConnect=false");

var rabbitMqOptions = RabbitMqOptions.FromEnvironment();

builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddSingleton(rabbitMqOptions);
builder.Services.AddSingleton<ITextRankCalculator, TextRankCalculator>();
builder.Services.AddSingleton<IRankStorage, RedisRankStorage>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
