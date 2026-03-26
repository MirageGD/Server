using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record InventorySlotDto
{
    [JsonPropertyName("item_id")]
    public required string ItemId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("texture")]
    public required string Texture { get; init; }

    [JsonPropertyName("sprite_index")]
    public required int SpriteIndex { get; init; }

    [JsonPropertyName("quantity")]
    public required int Quantity { get; init; }
}
