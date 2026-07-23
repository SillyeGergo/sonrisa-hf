using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Infrastructure.Generation;
using WorldEventAlerts.Api.Infrastructure.Messaging;
using WorldEventAlerts.Api.Infrastructure.Notifications;
using WorldEventAlerts.Api.Infrastructure.Repositories;
using WorldEventAlerts.Api.Workers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IAlertRuleRepository, InMemoryAlertRuleRepository>();
builder.Services.AddSingleton<INotificationLogRepository, InMemoryNotificationLogRepository>();
builder.Services.AddSingleton<IMockDataGenerator, BogusMockDataGenerator>();
builder.Services.AddSingleton<INotificationProvider, EmailNotificationProvider>();
builder.Services.AddSingleton<INotificationProvider, SlackNotificationProvider>();
builder.Services.AddSingleton(_ => new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        UseJitter = true
    })
    .AddTimeout(TimeSpan.FromSeconds(5))
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 3,
        BreakDuration = TimeSpan.FromSeconds(10)
    })
    .Build());
builder.Services.AddHostedService<EventProcessingWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();
