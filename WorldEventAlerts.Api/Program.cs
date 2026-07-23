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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
