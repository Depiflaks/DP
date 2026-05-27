using RankCalculator.Models;

namespace RankCalculator.Services;

public interface IEventPublisher
{
    Task PublishRankCalculatedAsync(string textId, double rank, CancellationToken cancellationToken = default);
}