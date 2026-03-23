using System.Text.Json.Serialization;
using GDMirage.Server.Features.Game.Entities;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Messages;

public class EntityDto
{
    [JsonPropertyName("entity_id")]
    public required int EntityId { get; init; }

    [JsonPropertyName("entity_type")]
    public required EntityType EntityType { get; init; }

    [JsonPropertyName("sprite")]
    public required string Sprite { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("direction")]
    public required Direction Direction { get; init; }

    [JsonPropertyName("x")]
    public required int X { get; init; }

    [JsonPropertyName("y")]
    public required int Y { get; init; }

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
