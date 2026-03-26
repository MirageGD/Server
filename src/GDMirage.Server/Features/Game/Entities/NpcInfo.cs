namespace GDMirage.Server.Features.Game.Entities;

public sealed record NpcInfo
{
    public string Name { get; init; } = string.Empty;
    public string Sprite { get; init; } = string.Empty;
    public int Level { get; init; } = 1;
    public int Strength { get; init; } = 10;
    public int Stamina { get; init; } = 10;
    public int Intelligence { get; init; } = 10;
    public double RespawnDelaySecs { get; init; } = 5.0;
    public Dictionary<string, double> Loot { get; init; } = [];
}
