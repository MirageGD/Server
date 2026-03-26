using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<ChatChannel>))]
public enum ChatChannel
{
    [JsonStringEnumMemberName("system")] System,
    [JsonStringEnumMemberName("combat")] Combat,
    [JsonStringEnumMemberName("local")] Local
}
