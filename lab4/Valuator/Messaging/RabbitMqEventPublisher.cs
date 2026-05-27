using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Valuator.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventPublisher(RabbitMqOptions options)
    {
        _options = options;
    }

    public async Task PublishSimilarityCalculatedAsync(string textId, double similarity, CancellationToken cancellationToken = default)
    {
        var similarityEvent = new SimilarityCalculatedEvent
        {
            TextId = textId,
            Similarity = similarity
        };

        await PublishEventAsync(similarityEvent, cancellationToken);
    }

    private async Task PublishEventAsync<T>(T eventData, CancellationToken cancellationToken) where T : class
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);

            var json = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel!.BasicPublishAsync(
                exchange: _options.EventsExchange,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null && _channel.IsOpen)
            return;

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.EventsExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
