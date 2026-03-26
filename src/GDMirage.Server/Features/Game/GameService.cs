using System.Collections.Concurrent;
using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Entities;
using GDMirage.Server.Features.Game.Messages;
using GDMirage.Server.Features.Shared;
using Microsoft.Extensions.Options;

namespace GDMirage.Server.Features.Game;

public sealed partial class GameService(
    ILogger<GameService> logger,
    IGameMapManager gameMapManager,
    EntityIdGenerator entityIdGenerator,
    ItemInfoManager itemInfoManager,
    IOptions<ServerOptions> serverOptions) : IGameService
{
    private readonly ConcurrentDictionary<int, Player> _players = new();

    public async Task<Player> CreatePlayerAsync(GameConnection connection, Account account, Character character)
    {
        var map = gameMapManager.GetMap(character.Map);

        var playerId = entityIdGenerator.GetNext();
        var player = new Player(playerId, connection, account.Name, character, map, itemInfoManager);

        if (!_players.TryAdd(playerId, player))
        {
            logger.LogWarning("Failed to add player '{CharacterName}' with EntityId {EntityId}", player.Name, playerId);
        }

        connection.Player = player;

        await SendMapInit(connection, character.Map, playerId, map);
        await map.AddEntityAsync(player);
        await player.SendXpAsync();
        await player.SendStatsAsync();
        await player.SendInventoryAsync();

        LogPlayerJoined(player.Name, playerId, character.Map);

        await connection.SendAsync("chat", new ChatMessage
        {
            Channel = ChatChannel.System,
            Message = $"Welcome to GDMirage! There are {_players.Count} players online!",
            Color = "#ffff00"
        });

        var joinMessage = new ChatMessage
        {
            Channel = ChatChannel.System,
            Message = $"{player.Name} has joined.",
            Color = "#ffff00"
        };

        foreach (var other in _players.Values)
        {
            if (other.EntityId == playerId) continue;
            await other.Connection.SendAsync("chat", joinMessage);
        }

        return player;
    }

    public async Task RemovePlayerAsync(int entityId)
    {
        if (!_players.TryRemove(entityId, out var player))
        {
            return;
        }

        await player.CurrentMap.RemoveEntityAsync(entityId);

        var leaveMessage = new ChatMessage
        {
            Channel = ChatChannel.System,
            Message = $"{player.Name} has left.",
            Color = "#ffff00"
        };

        foreach (var other in _players.Values)
        {
            await other.Connection.SendAsync("chat", leaveMessage);
        }

        LogPlayerLeft(player.Name, entityId);
    }

    private static async Task SendMapInit(GameConnection connection, string mapPath, int entityId, GameMap map)
    {
        var entities = map.GetAllEntities().Where(e => e.EntityId != entityId).ToList();
        var entityDtos = entities.Select(entity => new EntityDto
        {
            EntityId = entity.EntityId,
            EntityType = entity.Type,
            Sprite = entity.Sprite,
            Name = entity.Name,
            Level = entity.Level,
            MaxHealth = entity.MaxHealth,
            Health = entity.Health,
            MaxMana = entity.MaxMana,
            Mana = entity.Mana,
            Direction = entity.Direction,
            X = entity.X,
            Y = entity.Y,
        });

        var itemDtos = map.GetAllItems().Select(item => new ItemDto
        {
            InstanceId = item.InstanceId,
            Texture = item.Info.Texture,
            SpriteIndex = item.Info.SpriteIndex,
            X = item.X,
            Y = item.Y
        });

        await connection.SendAsync("map_init", new InitializeMap
        {
            MapPath = mapPath,
            PlayerEntityId = entityId,
            Entities = entityDtos.ToList(),
            Items = itemDtos.ToList()
        });
    }

    public Player? GetPlayer(int entityId)
    {
        return _players.TryGetValue(entityId, out var player) ? player : null;
    }

    public IEnumerable<Player> GetAllPlayers()
    {
        return _players.Values;
    }

    public async ValueTask SendToAllAsync<T>(string type, T payload)
    {
        foreach (var player in _players.Values)
        {
            await player.Connection.SendAsync(type, payload);
        }
    }

    public async ValueTask GrantExperience(int entityId, int amount)
    {
        if (_players.TryGetValue(entityId, out var player))
        {
            await player.GrantExperience(amount);
        }
    }

    public async Task RespawnPlayerAsync(Player player)
    {
        var options = serverOptions.Value;
        var startMap = gameMapManager.GetMap(options.StartMap);

        await player.CurrentMap.RemoveEntityAsync(player.EntityId);

        player.Health = player.MaxHealth;
        player.X = options.StartX;
        player.Y = options.StartY;
        player.CurrentMap = startMap;

        await SendMapInit(player.Connection, options.StartMap, player.EntityId, startMap);
        await startMap.AddEntityAsync(player);

        LogPlayerRespawned(player.Name, player.EntityId, options.StartMap);
    }

    public async Task WarpPlayerAsync(Player player)
    {
        if (!player.CurrentMap.TryGetWarpAt(player.X, player.Y, out var warp))
        {
            return;
        }

        Direction? targetDirection = null;
        if (warp.TargetDirection is not null && Enum.TryParse<Direction>(warp.TargetDirection, true, out var dir))
        {
            targetDirection = dir;
        }

        if (warp.TargetMap == player.CurrentMap.MapPath)
        {
            player.X = warp.TargetX;
            player.Y = warp.TargetY;

            if (targetDirection.HasValue)
            {
                player.Direction = targetDirection.Value;
            }

            await player.CurrentMap.BroadcastEntityPosition(player);
        }
        else
        {
            var targetMap = gameMapManager.GetMap(warp.TargetMap);

            await player.CurrentMap.RemoveEntityAsync(player.EntityId);

            player.X = warp.TargetX;
            player.Y = warp.TargetY;

            if (targetDirection.HasValue)
            {
                player.Direction = targetDirection.Value;
            }

            player.CurrentMap = targetMap;

            await SendMapInit(player.Connection, warp.TargetMap, player.EntityId, targetMap);
            await targetMap.AddEntityAsync(player);

            LogPlayerWarped(player.Name, player.EntityId, warp.TargetMap);
        }
    }

    public bool IsPlayerConnected(string accountName, string characterName)
    {
        return _players.Values.Any(player =>
            player.AccountName.Equals(accountName,
                StringComparison.OrdinalIgnoreCase) ||
            player.Name.Equals(characterName,
                StringComparison.OrdinalIgnoreCase));
    }

    [LoggerMessage(LogLevel.Information, "Player '{CharacterName}' (ID: {EntityId}) has joined the game on map '{MapPath}'")]
    partial void LogPlayerJoined(string characterName, int entityId, string mapPath);

    [LoggerMessage(LogLevel.Information, "Player '{CharacterName}' (ID: {EntityId}) has left the game")]
    partial void LogPlayerLeft(string characterName, int entityId);

    [LoggerMessage(LogLevel.Information, "Player '{CharacterName}' (ID: {EntityId}) has respawned on map '{MapPath}'")]
    partial void LogPlayerRespawned(string characterName, int entityId, string mapPath);

    [LoggerMessage(LogLevel.Information, "Player '{CharacterName}' (ID: {EntityId}) has warped to map '{MapPath}'")]
    partial void LogPlayerWarped(string characterName, int entityId, string mapPath);
}
