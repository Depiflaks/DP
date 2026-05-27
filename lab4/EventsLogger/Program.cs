using EventsLogger;

var builder = Host.CreateApplicationBuilder(args);

var rabbitMqOptions = RabbitMqOptions.FromEnvironment();

builder.Services.AddSingleton(rabbitMqOptions);
builder.Services.AddHostedService<EventsLoggerService>();

var host = builder.Build();

host.Run();