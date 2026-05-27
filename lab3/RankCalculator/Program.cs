using RankCalculator;
using RankCalculator.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ITextRankCalculator, TextRankCalculator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
