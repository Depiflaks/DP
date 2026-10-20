namespace EventsLogger;

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
