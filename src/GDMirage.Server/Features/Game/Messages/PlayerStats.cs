using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record PlayerStats
{
    [JsonPropertyName("strength")]
    public required int Strength { get; init; }

    [JsonPropertyName("stamina")]
    public required int Stamina { get; init; }

    [JsonPropertyName("intelligence")]
    public required int Intelligence { get; init; }

    [JsonPropertyName("stat_points")]
    public required int StatPoints { get; init; }
}
