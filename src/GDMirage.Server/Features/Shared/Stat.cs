using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Shared;

[JsonConverter(typeof(JsonStringEnumConverter<Stat>))]
public enum Stat
{
    [JsonStringEnumMemberName("strength")] Strength,
    [JsonStringEnumMemberName("stamina")] Stamina,
    [JsonStringEnumMemberName("intelligence")] Intelligence,
}
