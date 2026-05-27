namespace Valuator.Messaging;

public interface IMessagePublisher
{
    Task PublishRankCalculationAsync(string textId, CancellationToken cancellationToken = default);
    Task PublishSimilarityCalculatedAsync(string textId, double similarity, CancellationToken cancellationToken = default);
}