using RankCalculator;
using RankCalculator.Services;
using RankCalculator.Storage;
using Sharding;

var builder = Host.CreateApplicationBuilder(args);

var rabbitMqOptions = RankCalculator.RabbitMqOptions.FromEnvironment();

builder.Services.AddSingleton<RedisShardStore>();
builder.Services.AddSingleton(rabbitMqOptions);
builder.Services.AddSingleton<ITextRankCalculator, TextRankCalculator>();
builder.Services.AddSingleton<IRankStorage, RedisRankStorage>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
