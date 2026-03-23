using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record EntityHealth
{
    [JsonPropertyName("entity_id")]
    public required int EntityId { get; init; }

    [JsonPropertyName("health")]
    public required int Health { get; init; }

    [JsonPropertyName("max_health")]
    public required int MaxHealth { get; init; }
}
