using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record PlayerInventoryUpdate
{
    [JsonPropertyName("slot")]
    public required int Slot { get; init; }

    [JsonPropertyName("slot_data")]
    public InventorySlotDto? SlotData { get; init; }
}
