namespace GDMirage.Server.Features.Game.Entities;

public sealed record ItemInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Texture { get; init; } = string.Empty;
    public int SpriteIndex { get; init; }
    public ItemType Type { get; init; } = ItemType.None;
    public bool Stackable { get; init; }
    public int MaxStackSize { get; init; } = 1;
    public int? Damage { get; init; }
    public int? Defense { get; init; }
    public int Strength { get; init; }
    public int Stamina { get; init; }
    public int Intelligence { get; init; }
}
