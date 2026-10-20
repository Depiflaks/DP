namespace Valuator.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string QueueName { get; init; } = string.Empty;
    public string EventsExchange { get; init; } = string.Empty;

    public static RabbitMqOptions FromEnvironment()
    {
        return new RabbitMqOptions
        {
            Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = ReadPassword(),
            QueueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "rank-calculation",
            EventsExchange = Environment.GetEnvironmentVariable("RABBITMQ_EVENTS_EXCHANGE") ?? "events"
        };
    }

    private static string ReadPassword()
    {
        var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD");
        return !string.IsNullOrWhiteSpace(password)
            ? password
            : throw new InvalidOperationException("RABBITMQ_PASSWORD must be configured.");
    }
}
