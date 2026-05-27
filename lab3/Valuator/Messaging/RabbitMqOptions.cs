namespace Valuator.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string QueueName { get; init; } = string.Empty;

    public static RabbitMqOptions FromEnvironment()
    {
        return new RabbitMqOptions
        {
            Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
            QueueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "rank-calculation"
        };
    }
}