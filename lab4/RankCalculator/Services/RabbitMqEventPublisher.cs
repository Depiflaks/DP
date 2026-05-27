using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RankCalculator.Models;

namespace RankCalculator.Services;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly string _host;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _eventsExchange;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventPublisher(string host, string userName, string password, string eventsExchange = "events")
    {
        _host = host;
        _userName = userName;
        _password = password;
        _eventsExchange = eventsExchange;
    }

    public async Task PublishRankCalculatedAsync(string textId, double rank, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);

            var @event = new RankCalculatedEvent
            {
                TextId = textId,
                Rank = rank
            };

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel!.BasicPublishAsync(
                exchange: _eventsExchange,
                routingKey: "rank.calculated",
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
            HostName = _host,
            UserName = _userName,
            Password = _password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _eventsExchange,
            type: ExchangeType.Topic,
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
