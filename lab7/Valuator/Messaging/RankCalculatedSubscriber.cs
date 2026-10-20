using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Valuator.Hubs;

namespace Valuator.Messaging;

public sealed class RankCalculatedSubscriber(
    RabbitMqOptions options,
    IHubContext<SummaryHub> hub,
    ILogger<RankCalculatedSubscriber> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            UserName = options.UserName,
            Password = options.Password
        };
        _connection = await ConnectAsync(factory, stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var queueName = await ConfigureQueueAsync(stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, args) => ProcessEventAsync(args, stoppingToken);
        await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer,
            cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<IConnection> ConnectAsync(ConnectionFactory factory, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                return await factory.CreateConnectionAsync(ct);
            }
            catch (Exception exception) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(exception, "RabbitMQ connection failed");
                await Task.Delay(2000, ct);
            }
        }

        throw new OperationCanceledException(ct);
    }

    private async Task<string> ConfigureQueueAsync(CancellationToken ct)
    {
        await _channel!.ExchangeDeclareAsync(options.EventsExchange, ExchangeType.Fanout,
            durable: true, autoDelete: false, cancellationToken: ct);
        var queue = await _channel.QueueDeclareAsync(string.Empty, durable: false,
            exclusive: true, autoDelete: true, cancellationToken: ct);
        await _channel.QueueBindAsync(queue.QueueName, options.EventsExchange, string.Empty,
            cancellationToken: ct);
        return queue.QueueName;
    }

    private async Task ProcessEventAsync(BasicDeliverEventArgs args, CancellationToken ct)
    {
        try
        {
            using var document = JsonDocument.Parse(args.Body);
            var data = document.RootElement;
            if (data.GetProperty("EventType").GetString() != "RankCalculated")
            {
                return;
            }

            var textId = data.GetProperty("TextId").GetString();
            if (!Guid.TryParse(textId, out var id))
            {
                return;
            }

            await hub.Clients.Group(id.ToString()).SendAsync("RankCalculated", id.ToString(),
                data.GetProperty("Rank").GetDouble(), ct);
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            logger.LogError(exception, "Rank notification failed");
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
