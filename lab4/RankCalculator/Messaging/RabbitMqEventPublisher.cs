using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RankCalculator.Models;

namespace RankCalculator;

public interface IEventPublisher
{
    Task PublishRankCalculatedAsync(string textId, double rank, CancellationToken cancellationToken = default);
}

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

    public async Task PublishRankCalculatedAsync(string textId, double rank, CancellationToken cancellationToken = default)
    {
        var rankEvent = new RankCalculatedEvent
        {
            TextId = textId,
            Rank = rank
        };

        await PublishEventAsync(rankEvent, cancellationToken);
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

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string EventsExchange { get; init; } = string.Empty;

    public static RabbitMqOptions FromEnvironment()
    {
        return new RabbitMqOptions
        {
            Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
            EventsExchange = Environment.GetEnvironmentVariable("RABBITMQ_EVENTS_EXCHANGE") ?? "events"
        };
    }
}
