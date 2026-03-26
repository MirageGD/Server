using JetBrains.Annotations;

namespace GDMirage.Server.Features.Game.Entities;

[UsedImplicitly]
public sealed record ItemInfo : Info
{
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
