using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Accounts.Entities;

public sealed record Character
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public string Sprite { get; set; } = "sprites/spr_warrior.png";
    public int Health { get; set; } = 100;
    public int Mana { get; set; } = 100;
    public int StatPoints { get; set; }
    public int Strength { get; set; } = 10;
    public int Stamina { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Experience { get; set; }
    public string Map { get; set; } = "maps/test_map.tmj";
    public Direction Direction { get; set; } = Direction.Down;
    public int X { get; set; } = 19;
    public int Y { get; set; } = 8;
}
