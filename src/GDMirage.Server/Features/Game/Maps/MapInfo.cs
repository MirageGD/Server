namespace GDMirage.Server.Features.Game.Maps;

public sealed record MapInfo
{
    public string Name { get; init; } = string.Empty;
    public List<string> Npcs { get; init; } = [];
}
