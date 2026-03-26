using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record PlayerInventory
{
    [JsonPropertyName("size")]
    public required int Size { get; init; }

    [JsonPropertyName("slots")]
    public required Dictionary<int, InventorySlotDto> Slots { get; init; }
}
