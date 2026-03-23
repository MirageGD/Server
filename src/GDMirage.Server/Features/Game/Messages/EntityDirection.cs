using System.Text.Json.Serialization;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record EntityDirection
{
    [JsonPropertyName("entity_id")]
    public required int EntityId { get; init; }

    [JsonPropertyName("direction")]
    public required Direction Direction { get; init; }
}
