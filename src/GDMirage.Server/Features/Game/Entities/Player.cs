using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Messages;
using GDMirage.Server.Features.Game.Stats;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Entities;

public sealed class Player(int entityId, GameConnection connection, Character character, GameMap currentMap) : IEntity
{
    public int EntityId => entityId;
    public string Name => character.Name;

    public string Sprite
    {
        get => character.Sprite;
        set => character.Sprite = value;
    }

    public Direction Direction
    {
        get => character.Direction;
        set => character.Direction = value;
    }

    public int X
    {
        get => character.X;
        set => character.X = value;
    }

    public int Y
    {
        get => character.Y;
        set => character.Y = value;
    }

    public EntityType Type => EntityType.Player;

    public int Level => character.Level;

    public int Health
    {
        get => character.Health;
        set => character.Health = value;
    }

    public int MaxHealth => character.MaxHealth;

    public int Mana
    {
        get => character.Mana;
        set => character.Mana = value;
    }

    public int MaxMana => character.MaxMana;
    public int Strength => character.Strength;
    public int Stamina => character.Stamina;
    public int Intelligence => character.Intelligence;

    public GameConnection Connection { get; } = connection;
    public GameMap CurrentMap { get; set; } = currentMap;

    public async ValueTask GrantExperience(int amount)
    {
        character.Experience += amount;

        while (character.Experience >= ExperienceCalculator.CalculateExperienceForLevel(Level))
        {
            await LevelUp();
        }

        await SendXpAsync();
    }

    private async ValueTask LevelUp()
    {
        var overflow = character.Experience - ExperienceCalculator.CalculateExperienceForLevel(Level);

        character.Level++;
        character.Experience = overflow;
        character.MaxHealth = VitalCalculator.CalculateMaxHealth(character.Stamina, character.Level);
        character.Health = character.MaxHealth;
        character.MaxMana = VitalCalculator.CalculateMaxMana(character.Intelligence, character.Level);
        character.Mana = character.MaxMana;
        character.StatPoints += 5;

        await CurrentMap.BroadcastEntityLeveledUp(this);

        await Connection.SendAsync("chat", new ChatMessage
        {
            Channel = "system",
            Message = $"You have reached level {character.Level}!",
            Color = "#ffff00"
        });
    }

    public ValueTask SendXpAsync()
    {
        return Connection.SendAsync("player_xp", new PlayerXp
        {
            Experience = character.Experience,
            ExperienceRequired = ExperienceCalculator.CalculateExperienceForLevel(Level)
        });
    }
}
