using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public record ItemDto
{
    [JsonPropertyName("instance_id")] public required int InstanceId { get; init; }
    [JsonPropertyName("texture")] public required string Texture { get; init; }
    [JsonPropertyName("sprite_index")] public required int SpriteIndex { get; init; }
    [JsonPropertyName("x")] public required int X { get; init; }
    [JsonPropertyName("y")] public required int Y { get; init; }
}
