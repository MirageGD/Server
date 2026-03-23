using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Entities;

public interface IEntity
{
    int EntityId { get; }
    EntityType Type { get; }
    string Sprite { get; }
    string Name { get; }
    int Level { get; }
    int Health { get; set; }
    int MaxHealth { get; }
    int Mana { get; set; }
    int MaxMana { get; }
    int Strength { get; }
    int Stamina { get; }
    int Intelligence { get; }
    Direction Direction { get; set; }
    int X { get; set; }
    int Y { get; set; }
}
