using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record InitializeMap
{
    [JsonPropertyName("map_path")]
    public required string MapPath { get; init; }

    [JsonPropertyName("player_entity_id")]
    public required int PlayerEntityId { get; init; }

    [JsonPropertyName("entities")]
    public required List<EntityDto> Entities { get; init; }
}
