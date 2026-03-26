using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Entities;

public abstract record Info
{
    [JsonIgnore]
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
