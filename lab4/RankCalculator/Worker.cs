using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RankCalculator.Models;
using RankCalculator.Services;
using RankCalculator.Storage;

namespace RankCalculator;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITextRankCalculator _textRankCalculator;
    private readonly IRankStorage _rankStorage;
    private readonly IEventPublisher _eventPublisher;

    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        ILogger<Worker> logger,
        ITextRankCalculator textRankCalculator,
        IRankStorage rankStorage,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _textRankCalculator = textRankCalculator;
        _rankStorage = rankStorage;
        _eventPublisher = eventPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "rank-calculation";
        await ConnectAsync(stoppingToken);
        await ConfigureQueueAsync(queueName, stoppingToken);
        await StartConsumerAsync(queueName, stoppingToken);

        _logger.LogInformation("RankCalculator is listening queue {QueueName}", queueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest"
        };

        _connection = await CreateConnectionWithRetryAsync(factory, cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    private async Task ConfigureQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        await _channel!.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);
    }

    private async Task StartConsumerAsync(string queueName, CancellationToken cancellationToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += (_, eventArgs) => ProcessMessageAsync(eventArgs, cancellationToken);

        await _channel!.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
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

    private async Task ProcessMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            await CalculateRankAsync(eventArgs, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Message processing failed");
            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: cancellationToken);
        }
    }

    private async Task CalculateRankAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        var message = JsonSerializer.Deserialize<RankCalculationMessage>(json);
        if (message is null || string.IsNullOrWhiteSpace(message.TextId))
        {
            await RejectMessageAsync(eventArgs.DeliveryTag, cancellationToken);
            return;
        }

        var text = await _rankStorage.GetTextAsync(message.TextId);
        if (text is null)
        {
            await RejectMessageAsync(eventArgs.DeliveryTag, cancellationToken);
            return;
        }

        await SaveRankAsync(message.TextId, text, eventArgs.DeliveryTag, cancellationToken);
    }

    private async Task SaveRankAsync(
        string textId, string text, ulong deliveryTag, CancellationToken cancellationToken)
    {
        var rank = _textRankCalculator.CalculateRank(text);
        await _rankStorage.SaveRankAsync(textId, rank);
        await _eventPublisher.PublishRankCalculatedAsync(textId, rank, cancellationToken);

        await _channel!.BasicAckAsync(
            deliveryTag: deliveryTag,
            multiple: false,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Rank calculated for text {TextId}: {Rank}", textId, rank);
    }

    private async Task RejectMessageAsync(ulong deliveryTag, CancellationToken cancellationToken)
    {
        await _channel!.BasicRejectAsync(
            deliveryTag: deliveryTag,
            requeue: false,
            cancellationToken: cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}
