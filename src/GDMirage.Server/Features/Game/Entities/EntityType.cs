using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Entities;

[JsonConverter(typeof(JsonStringEnumConverter<EntityType>))]
public enum EntityType
{
    [JsonStringEnumMemberName("player")] Player,
    [JsonStringEnumMemberName("npc")] Npc
}
