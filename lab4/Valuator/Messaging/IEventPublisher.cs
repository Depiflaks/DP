namespace Valuator.Messaging;

public interface IEventPublisher
{
    Task PublishSimilarityCalculatedAsync(string textId, double similarity, CancellationToken cancellationToken = default);
}
