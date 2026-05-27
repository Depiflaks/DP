using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventsLogger;

public class EventsLoggerService : BackgroundService
{
    private readonly ILogger<EventsLoggerService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public EventsLoggerService(ILogger<EventsLoggerService> logger, RabbitMqOptions options)
    {
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = await CreateConnectionWithRetryAsync(factory, stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.EventsExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        var queueName = await _channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await _channel.QueueBindAsync(
            queue: queueName.QueueName,
            exchange: _options.EventsExchange,
            routingKey: string.Empty,
            arguments: null,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await ProcessEventAsync(eventArgs, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: queueName.QueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("EventsLogger started listening to exchange {ExchangeName}", _options.EventsExchange);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<IConnection> CreateConnectionWithRetryAsync(
        ConnectionFactory factory,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                return await factory.CreateConnectionAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "RabbitMQ connection failed");
                await Task.Delay(2000, cancellationToken);
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private async Task ProcessEventAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        try
        {
            var body = eventArgs.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            if (root.TryGetProperty("EventType", out var eventTypeElement))
            {
                var eventType = eventTypeElement.GetString();

                if (eventType == "RankCalculated")
                {
                    LogRankCalculatedEvent(root);
                }
                else if (eventType == "SimilarityCalculated")
                {
                    LogSimilarityCalculatedEvent(root);
                }
                else
                {
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Event processing failed");
        }
    }

    private void LogRankCalculatedEvent(JsonElement eventData)
    {
        var eventType = eventData.GetProperty("EventType").GetString();
        var textId = eventData.GetProperty("TextId").GetString();
        var rank = eventData.GetProperty("Rank").GetDouble();

        _logger.LogInformation(
            "Event Type: {EventType} | Text ID: {TextId} | Rank: {Rank}",
            eventType,
            textId,
            rank);
    }

    private void LogSimilarityCalculatedEvent(JsonElement eventData)
    {
        var eventType = eventData.GetProperty("EventType").GetString();
        var textId = eventData.GetProperty("TextId").GetString();
        var similarity = eventData.GetProperty("Similarity").GetDouble();

        _logger.LogInformation(
            "Event Type: {EventType} | Text ID: {TextId} | Similarity: {Similarity}",
            eventType,
            textId,
            similarity);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}