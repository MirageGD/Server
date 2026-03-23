using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record EntityDeath
{
    [JsonPropertyName("entity_id")]
    public required int EntityId { get; init; }
}
