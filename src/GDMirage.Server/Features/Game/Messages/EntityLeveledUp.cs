using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record EntityLeveledUp
{
    [JsonPropertyName("entity_id")]
    public required int EntityId { get; init; }

    [JsonPropertyName("level")]
    public required int Level { get; init; }

    [JsonPropertyName("max_health")]
    public required int MaxHealth { get; init; }

    [JsonPropertyName("health")]
    public required int Health { get; init; }

    [JsonPropertyName("max_mana")]
    public required int MaxMana { get; init; }

    [JsonPropertyName("mana")]
    public required int Mana { get; init; }
}
