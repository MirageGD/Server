using System.Text.Json.Serialization;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Messages;

public sealed class EntityMoved
{
    [JsonPropertyName("entity_id")]
    public required int EntityId { get; init; }

    [JsonPropertyName("direction")]
    public required Direction Direction { get; init; }

    [JsonPropertyName("source_x")]
    public required int SourceX { get; init; }

    [JsonPropertyName("source_y")]
    public required int SourceY { get; init; }
    
    [JsonPropertyName("speed")]
    public required float Speed { get; init; }
}
