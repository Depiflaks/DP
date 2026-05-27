using RankCalculator.Models;

namespace RankCalculator;

public interface IEventPublisher
{
    Task PublishRankCalculatedAsync(string textId, double rank, CancellationToken cancellationToken = default);
}