using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record ItemRemoved
{
    [JsonPropertyName("instance_id")] public required int InstanceId { get; init; }
}
