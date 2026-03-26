using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Messages;
using GDMirage.Server.Features.Game.Stats;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Entities;

public sealed class Player(int entityId, GameConnection connection, string accountName, Character character, GameMap currentMap, InfoManager<ItemInfo> itemInfoManager) : IEntity
{
    public int EntityId => entityId;
    public string AccountName => accountName;
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

    public int MaxHealth => VitalCalculator.CalculateMaxHealth(character.Stamina, character.Level);
    public int MaxMana => VitalCalculator.CalculateMaxMana(character.Intelligence, character.Level);

    public int Health
    {
        get => character.Health;
        set => character.Health = value;
    }

    public int Mana
    {
        get => character.Mana;
        set => character.Mana = value;
    }

    public int Strength => character.Strength;
    public int Stamina => character.Stamina;
    public int Intelligence => character.Intelligence;

    public GameConnection Connection { get; } = connection;
    public Inventory Inventory { get; } = new(character, itemInfoManager);

    public GameMap CurrentMap
    {
        get;
        set
        {
            field = value;

            character.Map = value.Id;
        }
    } = currentMap;

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
        character.Health = MaxHealth;
        character.Mana = MaxMana;
        character.StatPoints += 5;

        await CurrentMap.BroadcastEntityLeveledUp(this);
        await SendStatsAsync();

        await Connection.SendAsync("chat", new ChatMessage
        {
            Channel = ChatChannel.System,
            Message = $"You have reached level {character.Level}!",
            Color = "#ffff00"
        });
    }

    public async ValueTask UseStatPoint(Stat stat)
    {
        if (character.StatPoints == 0)
        {
            return;
        }

        switch (stat)
        {
            case Stat.Strength:
                character.Strength++;
                break;

            case Stat.Stamina:
                character.Stamina++;
                break;

            case Stat.Intelligence:
                character.Intelligence++;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }

        character.StatPoints--;

        await SendStatsAsync();

        if (stat == Stat.Stamina)
        {
            await CurrentMap.SendToAllAsync("entity_health", new EntityHealth
            {
                EntityId = EntityId,
                Health = Health,
                MaxHealth = MaxHealth
            });
        }
    }

    public ValueTask SendPositionAsync()
    {
        return Connection.SendAsync("entity_position", new EntityPosition
        {
            EntityId = EntityId,
            Direction = Direction,
            X = X,
            Y = Y
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

    public ValueTask SendStatsAsync()
    {
        return Connection.SendAsync("player_stats", new PlayerStats
        {
            Strength = character.Strength,
            Stamina = character.Stamina,
            Intelligence = character.Intelligence,
            StatPoints = character.StatPoints
        });
    }

    public ValueTask SendInventoryAsync()
    {
        var slots = Inventory.GetAllSlotDtos().ToDictionary(x => x.Index, x => x.Dto);

        return Connection.SendAsync("player_inventory", new PlayerInventory
        {
            Size = Inventory.Size,
            Slots = slots
        });
    }

    public ValueTask SendInventoryUpdateAsync(int slot)
    {
        return Connection.SendAsync("player_inventory_update", new PlayerInventoryUpdate
        {
            Slot = slot,
            SlotData = Inventory.GetSlotDto(slot)
        });
    }
}
