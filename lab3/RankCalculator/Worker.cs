using RankCalculator.Services;

namespace RankCalculator;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITextRankCalculator _textRankCalculator;

    public Worker(ILogger<Worker> logger, ITextRankCalculator textRankCalculator)
    {
        _logger = logger;
        _textRankCalculator = textRankCalculator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RankCalculator started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}