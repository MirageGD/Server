using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Entities;

[JsonConverter(typeof(JsonStringEnumConverter<ItemType>))]
public enum ItemType
{
    [JsonStringEnumMemberName("none")] None,
    [JsonStringEnumMemberName("weapon")] Weapon,
    [JsonStringEnumMemberName("armor")] Armor,
    [JsonStringEnumMemberName("helmet")] Helmet
}
