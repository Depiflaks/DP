using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Valuator.Messaging;

public sealed class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqMessagePublisher(RabbitMqOptions options)
    {
        _options = options;
    }

    public async Task PublishRankCalculationAsync(string textId, CancellationToken cancellationToken = default)
    {
        var message = new RankCalculationMessage
        {
            TextId = textId
        };

        await PublishInternalAsync(
            exchange: string.Empty, 
            routingKey: _options.QueueName, 
            message: message, 
            cancellationToken: cancellationToken
        );
    }

    public async Task PublishSimilarityCalculatedAsync(string textId, double similarity, CancellationToken cancellationToken = default)
    {
        var similarityEvent = new SimilarityCalculatedEvent
        {
            TextId = textId,
            Similarity = similarity
        };

        await PublishInternalAsync(
            exchange: _options.EventsExchange, 
            routingKey: string.Empty, 
            message: similarityEvent, 
            cancellationToken: cancellationToken
        );
    }

    private async Task PublishInternalAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel!.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
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

        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

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