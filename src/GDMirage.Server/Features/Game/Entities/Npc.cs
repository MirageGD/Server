using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Entities;

public sealed class Npc(int entityId, NpcInfo info) : IEntity
{
    public int EntityId { get; } = entityId;
    public EntityType Type => EntityType.Npc;

    public string Sprite => info.Sprite;
    public string Name => info.Name;
    public int Level => info.Level;
    public int Strength => info.Strength;
    public int Stamina => info.Stamina;
    public int Intelligence => info.Intelligence;
    public double RespawnDelaySec => info.RespawnDelaySecs;
    public Dictionary<string, double> Loot => info.Loot;

    public int MaxHealth { get; init; }
    public int Health { get; set; }
    public int MaxMana { get; init; }
    public int Mana { get; set; }

    public Direction Direction { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public int? TargetEntityId { get; set; }
    public DateTime? LastAttackTime { get; set; }
    public DateTime? LastMoveTime { get; set; }
}
