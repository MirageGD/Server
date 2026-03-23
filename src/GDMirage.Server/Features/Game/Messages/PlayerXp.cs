using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record PlayerXp
{
    [JsonPropertyName("xp")] public int Experience { get; set; }
    [JsonPropertyName("xp_required")] public int ExperienceRequired { get; set; }
}
