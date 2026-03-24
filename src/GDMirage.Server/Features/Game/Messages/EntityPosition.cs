using System.Text.Json.Serialization;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Messages;

public sealed class EntityPosition
{
    [JsonPropertyName("entity_id")]
    public required int EntityId { get; init; }

    [JsonPropertyName("direction")]
    public required Direction Direction { get; init; }
    
    [JsonPropertyName("x")]
    public required int X { get; init; }

    [JsonPropertyName("y")]
    public required int Y { get; init; }
}
