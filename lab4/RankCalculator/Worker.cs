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

    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        ILogger<Worker> logger,
        ITextRankCalculator textRankCalculator,
        IRankStorage rankStorage)
    {
        _logger = logger;
        _textRankCalculator = textRankCalculator;
        _rankStorage = rankStorage;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
        var queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "rank-calculation";

        var factory = new ConnectionFactory
        {
            HostName = rabbitHost,
            UserName = rabbitUser,
            Password = rabbitPassword
        };

        _connection = await CreateConnectionWithRetryAsync(factory, stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await ProcessMessageAsync(eventArgs, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("RankCalculator is listening queue {QueueName}", queueName);

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

    private async Task ProcessMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
            return;

        try
        {
            var body = eventArgs.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<RankCalculationMessage>(json);

            if (message is null || string.IsNullOrWhiteSpace(message.TextId))
            {
                await _channel.BasicRejectAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    requeue: false,
                    cancellationToken: cancellationToken
                );

                return;
            }

            var text = await _rankStorage.GetTextAsync(message.TextId);

            if (text is null)
            {
                await _channel.BasicRejectAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    requeue: false,
                    cancellationToken: cancellationToken
                );

                return;
            }

            var rank = _textRankCalculator.CalculateRank(text);

            await _rankStorage.SaveRankAsync(message.TextId, rank);

            await _channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Rank calculated for text {TextId}: {Rank}", message.TextId, rank);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Message processing failed");

            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: cancellationToken
            );
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}