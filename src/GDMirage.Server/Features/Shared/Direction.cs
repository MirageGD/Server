using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Shared;

[JsonConverter(typeof(JsonStringEnumConverter<Direction>))]
public enum Direction
{
    [JsonStringEnumMemberName("up")] Up,
    [JsonStringEnumMemberName("down")] Down,
    [JsonStringEnumMemberName("left")] Left,
    [JsonStringEnumMemberName("right")] Right
}
