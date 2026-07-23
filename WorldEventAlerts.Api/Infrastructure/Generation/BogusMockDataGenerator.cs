using Bogus;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Infrastructure.Generation;

public sealed class BogusMockDataGenerator : IMockDataGenerator
{
    private static readonly string[] EventTypes =
    [
        "BreakingNews",
        "MarketMovement",
        "NaturalDisaster",
        "WeatherAlert",
        "SecurityIncident"
    ];

    private static readonly string[] Sources =
    [
        "Reuters",
        "Bloomberg",
        "WeatherService",
        "EmergencyFeed",
        "SecurityOps"
    ];

    private static readonly string[] MatchExpressions =
    [
        "Breaking",
        "Market",
        "Disaster",
        "Alert",
        "Incident",
        "Storm",
        "Flood",
        "Earthquake"
    ];

    private static readonly string[] Channels =
    [
        "Email",
        "Slack"
    ];

    public IReadOnlyCollection<WorldEvent> GenerateWorldEvents(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<WorldEvent>();
        }

        var faker = new Faker();

        return Enumerable.Range(0, count)
            .Select(_ => new WorldEvent
            {
                Id = Guid.NewGuid(),
                EventType = faker.PickRandom(EventTypes),
                Source = faker.PickRandom(Sources),
                PayloadJson = faker.Random.Bool()
                    ? System.Text.Json.JsonSerializer.Serialize(new
                    {
                        title = faker.Lorem.Sentence(),
                        severity = faker.PickRandom("Low", "Medium", "High", "Critical"),
                        summary = faker.Lorem.Paragraph()
                    })
                    : System.Text.Json.JsonSerializer.Serialize(new
                    {
                        title = faker.Company.CatchPhrase(),
                        value = faker.Finance.Amount(1000, 100000),
                        currency = "USD"
                    }),
                OccurredAtUtc = faker.Date.RecentOffset(7),
                Metadata = new Dictionary<string, string>
                {
                    ["region"] = faker.Address.StateAbbr(),
                    ["category"] = faker.PickRandom(EventTypes),
                    ["priority"] = faker.PickRandom("P1", "P2", "P3")
                }
            })
            .ToArray();
    }

    public IReadOnlyCollection<AlertRule> GenerateAlertRules(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<AlertRule>();
        }

        var faker = new Faker();

        return Enumerable.Range(0, count)
            .Select(index => new AlertRule
            {
                Id = Guid.NewGuid(),
                Name = $"Auto Rule {index + 1}: {faker.Hacker.Verb()} {faker.Hacker.Noun()}",
                IsActive = true,
                MatchExpression = faker.PickRandom(MatchExpressions),
                NotificationChannels = faker.PickRandom(Channels, faker.Random.Int(1, Channels.Length)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                Description = faker.Lorem.Sentence(),
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-faker.Random.Int(1, 5000)),
                UpdatedAtUtc = null
            })
            .ToArray();
    }
}