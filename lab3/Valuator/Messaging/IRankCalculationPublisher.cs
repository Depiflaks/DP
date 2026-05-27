namespace Valuator.Messaging;

public interface IRankCalculationPublisher
{
    Task PublishAsync(string textId, CancellationToken cancellationToken = default);
}
