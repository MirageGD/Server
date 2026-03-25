using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using GDMirage.Server.Features.Game.Combat;
using GDMirage.Server.Features.Game.Entities;
using GDMirage.Server.Features.Game.Maps;
using GDMirage.Server.Features.Game.Maps.Objects;
using GDMirage.Server.Features.Game.Messages;
using GDMirage.Server.Features.Game.Stats;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game;

public sealed partial class GameMap
{
    private const double HealthRegenIntervalInSeconds = 2.0;

    private readonly ConcurrentDictionary<int, IEntity> _entities = new();
    private readonly ConcurrentQueue<(Npc Npc, DateTime RespawnAt)> _pendingRespawns = new();
    private readonly ILogger _logger;
    private readonly IGameService _gameService;
    private readonly Map _map;
    private readonly Func<int> _entityIdGenerator;
    private readonly IReadOnlyList<NpcInfo> _npcInfoList;
    private readonly Random _random = new();
    private DateTime _lastRegenTime = DateTime.UtcNow;

    public string MapPath { get; }

    public GameMap(ILogger logger, IGameService gameService, string mapPath, string fullPath, Func<int> entityIdGenerator, IReadOnlyList<NpcInfo> npcInfoList)
    {
        _logger = logger;
        _gameService = gameService;
        _map = new Map(fullPath);
        _entityIdGenerator = entityIdGenerator;
        _npcInfoList = npcInfoList;

        MapPath = mapPath;

        LogMapLoaded(mapPath);

        SpawnNpcs();
    }

    private void SpawnNpcs()
    {
        foreach (var npcInfo in _npcInfoList)
        {
            var (x, y) = FindRandomPassableTile();

            var npc = new Npc(_entityIdGenerator(), npcInfo)
            {
                Direction = (Direction)_random.Next(0, 4),
                X = x,
                Y = y,
                MaxHealth = VitalCalculator.CalculateMaxHealth(npcInfo.Stamina, npcInfo.Level),
                MaxMana = VitalCalculator.CalculateMaxMana(npcInfo.Intelligence, npcInfo.Level)
            };

            npc.Health = npc.MaxHealth;
            npc.Mana = npc.MaxMana;

            _entities.TryAdd(npc.EntityId, npc);

            LogEntityAdded(npc.Name, npc.EntityId, MapPath);
        }
    }

    private (int x, int y) FindRandomPassableTile()
    {
        const int maxAttempts = 100;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var x = _random.Next(0, _map.Width);
            var y = _random.Next(0, _map.Height);

            if (_map.IsNpcPassable(x, y))
            {
                return (x, y);
            }
        }

        for (var y = 0; y < _map.Height; y++)
        {
            for (var x = 0; x < _map.Width; x++)
            {
                if (_map.IsNpcPassable(x, y))
                {
                    return (x, y);
                }
            }
        }

        return (0, 0);
    }

    public bool TryGetWarpAt(int x, int y, [NotNullWhen(true)] out Warp? warp) => _map.TryGetWarpAt(x, y, out warp);

    public async ValueTask WarpAsync(int entityId)
    {
        if (_entities.TryGetValue(entityId, out var entity) && entity is Player player)
        {
            await _gameService.WarpPlayerAsync(player);
        }
    }

    public ValueTask AddEntityAsync(IEntity entity)
    {
        if (!_entities.TryAdd(entity.EntityId, entity))
        {
            return ValueTask.CompletedTask;
        }

        LogEntityAdded(entity.Name, entity.EntityId, MapPath);

        return BroadcastEntityJoined(entity);
    }

    public ValueTask RemoveEntityAsync(int entityId)
    {
        if (!_entities.TryRemove(entityId, out var entity))
        {
            return ValueTask.CompletedTask;
        }

        LogEntityRemoved(entity.Name, entityId, MapPath);

        return BroadcastEntityLeft(entity);
    }

    public async ValueTask MoveEntityAsync(int entityId, Direction direction)
    {
        if (!_entities.TryGetValue(entityId, out var entity))
        {
            return;
        }

        var sourceX = entity.X;
        var sourceY = entity.Y;

        var (newX, newY) = direction switch
        {
            Direction.Up => (entity.X, entity.Y - 1),
            Direction.Down => (entity.X, entity.Y + 1),
            Direction.Left => (entity.X - 1, entity.Y),
            Direction.Right => (entity.X + 1, entity.Y),
            _ => (entity.X, entity.Y)
        };

        if (!_map.IsPassable(newX, newY) || IsTileOccupied(entity, newX, newY))
        {
            if (entity is Player player)
            {
                await player.SendPositionAsync();
            }

            return;
        }

        entity.Direction = direction;
        entity.X = newX;
        entity.Y = newY;

        await BroadcastEntityMoved(entity, sourceX, sourceY, direction);
    }

    public ValueTask UpdateDirectionAsync(int entityId, Direction direction)
    {
        if (!_entities.TryGetValue(entityId, out var entity))
        {
            return ValueTask.CompletedTask;
        }

        entity.Direction = direction;

        return SendToAllAsync("entity_direction", new EntityDirection
        {
            EntityId = entityId,
            Direction = direction
        });
    }

    public async ValueTask AttackAsync(int attackerId)
    {
        if (!_entities.TryGetValue(attackerId, out var attacker))
        {
            return;
        }

        var (targetX, targetY) = attacker.GetTileFacing();

        var target = _entities.Values.FirstOrDefault(e => e.X == targetX && e.Y == targetY);
        if (target is null)
        {
            return;
        }

        await SendToAllAsync("entity_attack", new EntityAttack
        {
            EntityId = attackerId,
            Direction = attacker.Direction
        });

        var damage = DamageCalculator.CalculateMeleeDamage(attacker.Strength);

        target.Health = Math.Max(0, target.Health - damage);

        await SendToAllAsync("entity_hurt", new EntityHurt
        {
            EntityId = target.EntityId,
            Damage = damage,
            Health = target.Health,
            MaxHealth = target.MaxHealth
        });

        switch (attacker)
        {
            case Player attackerPlayer when target is Npc hitNpc:
                await attackerPlayer.Connection.SendAsync("chat", new ChatMessage
                {
                    Channel = "combat",
                    Message = $"You hit {hitNpc.Name} for {damage}!",
                    Color = "#ff8800"
                });
                break;

            case Npc attackingNpc when target is Player hitPlayer:
                await hitPlayer.Connection.SendAsync("chat", new ChatMessage
                {
                    Channel = "combat",
                    Message = $"A {attackingNpc.Name} hits you for {damage}!",
                    Color = "#ff4444"
                });
                break;
        }

        if (target.Health <= 0)
        {
            await SendToAllAsync("entity_death", new EntityDeath
            {
                EntityId = target.EntityId
            });

            if (attacker is Player killerPlayer && target is Npc killedNpc)
            {
                var experience = killedNpc.Level * 20;
                var variance = _random.NextDouble() * 0.3 - 0.15;

                experience = Math.Max(1, (int)(experience * (1 + variance)));

                await _gameService.GrantExperience(attackerId, experience);

                await killerPlayer.Connection.SendAsync("chat", new ChatMessage
                {
                    Channel = "combat",
                    Message = $"You defeated a {killedNpc.Name} and received {experience} XP points!",
                    Color = "#ffff00"
                });

                _pendingRespawns.Enqueue((killedNpc, DateTime.UtcNow.AddSeconds(killedNpc.RespawnDelaySec)));

                await RemoveEntityAsync(killedNpc.EntityId);
            }
            else if (target is Player killedPlayer)
            {
                if (attacker is Npc killerNpc)
                {
                    killerNpc.TargetEntityId = null;

                    await killedPlayer.Connection.SendAsync("chat", new ChatMessage
                    {
                        Channel = "combat",
                        Message = $"You have been killed by a {killerNpc.Name}!",
                        Color = "#ff0000"
                    });

                    foreach (var entity in _entities.Values)
                    {
                        if (entity is not Player otherPlayer || otherPlayer.EntityId == killedPlayer.EntityId) continue;

                        await otherPlayer.Connection.SendAsync("chat", new ChatMessage
                        {
                            Channel = "local",
                            Message = $"{killedPlayer.Name} has been killed by a {killerNpc.Name}.",
                            Color = "#ff8800"
                        });
                    }
                }

                await _gameService.RespawnPlayerAsync(killedPlayer);
            }
            else
            {
                await RemoveEntityAsync(target.EntityId);
            }
        }
        else
        {
            if (target is Npc targetNpc)
            {
                targetNpc.TargetEntityId = attackerId;
            }
        }
    }

    public ValueTask BroadcastLocalChatAsync(Player sender, string message)
    {
        LogChat(sender.Name, message);

        return SendToAllAsync("chat", new ChatMessage
        {
            Channel = "local",
            EntityId = sender.EntityId,
            SenderName = sender.Name,
            Message = message,
            Color = "#ffffff"
        });
    }

    private ValueTask BroadcastEntityJoined(IEntity entity)
    {
        return SendToAllAsync("entity_joined", new EntityJoined
        {
            EntityId = entity.EntityId,
            EntityType = entity.Type,
            Sprite = entity.Sprite,
            Name = entity.Name,
            Direction = entity.Direction,
            X = entity.X,
            Y = entity.Y,
            Level = entity.Level,
            Health = entity.Health,
            MaxHealth = entity.MaxHealth,
            Mana = entity.Mana,
            MaxMana = entity.MaxMana
        });
    }

    public ValueTask BroadcastEntityLeveledUp(IEntity entity)
    {
        return SendToAllAsync("entity_leveled_up", new EntityLeveledUp
        {
            EntityId = entity.EntityId,
            Level = entity.Level,
            Health = entity.Health,
            MaxHealth = entity.MaxHealth,
            Mana = entity.Mana,
            MaxMana = entity.MaxMana
        });
    }

    private ValueTask BroadcastEntityLeft(IEntity entity)
    {
        return SendToAllAsync("entity_left", new EntityLeft
        {
            EntityId = entity.EntityId
        });
    }

    public ValueTask BroadcastEntityPosition(IEntity entity)
    {
        return SendToAllAsync("entity_position", new EntityPosition
        {
            EntityId = entity.EntityId,
            Direction = entity.Direction,
            X = entity.X,
            Y = entity.Y
        });
    }

    private ValueTask BroadcastEntityMoved(IEntity entity, int sourceX, int sourceY, Direction direction)
    {
        return SendToAllAsync("entity_moved", new EntityMoved
        {
            EntityId = entity.EntityId,
            Direction = direction,
            SourceX = sourceX,
            SourceY = sourceY,
            Speed = 0.2f
        });
    }

    private async ValueTask SendToAllAsync<T>(string type, T payload)
    {
        foreach (var entity in _entities.Values)
        {
            if (entity is not Player player) continue;

            try
            {
                await player.Connection.SendAsync(type, payload);
            }
            catch (ChannelClosedException)
            {
            }
        }
    }

    public IEnumerable<IEntity> GetAllEntities()
    {
        return _entities.Values;
    }

    public async Task UpdateAsync()
    {
        if (!_entities.Values.OfType<Player>().Any())
        {
            return;
        }

        await ProcessNpcRespawnsAsync();

        var now = DateTime.UtcNow;
        if ((now - _lastRegenTime).TotalSeconds >= HealthRegenIntervalInSeconds)
        {
            _lastRegenTime = now;

            await RegenHealthAsync();
        }

        var npcs = _entities.Values.OfType<Npc>().ToList();

        foreach (var npc in npcs)
        {
            if (npc.TargetEntityId.HasValue)
            {
                if (!_entities.TryGetValue(npc.TargetEntityId.Value, out var target))
                {
                    npc.TargetEntityId = null;
                    continue;
                }

                var dx = Math.Abs(target.X - npc.X);
                var dy = Math.Abs(target.Y - npc.Y);

                var isAdjacent = (dx == 1 && dy == 0) || (dx == 0 && dy == 1);

                if (isAdjacent)
                {
                    if (target.X < npc.X) npc.Direction = Direction.Left;
                    else if (target.X > npc.X) npc.Direction = Direction.Right;
                    else if (target.Y < npc.Y) npc.Direction = Direction.Up;
                    else if (target.Y > npc.Y) npc.Direction = Direction.Down;

                    if (npc.LastAttackTime.HasValue && !((now - npc.LastAttackTime.Value).TotalSeconds >= 0.7))
                    {
                        continue;
                    }

                    npc.LastAttackTime = now;

                    await AttackAsync(npc.EntityId);
                }
                else
                {
                    var direction = FindPathNextStep(npc.X, npc.Y, target.X, target.Y);
                    if (!direction.HasValue)
                    {
                        continue;
                    }

                    npc.LastMoveTime = now;

                    await MoveEntityAsync(npc.EntityId, direction.Value);
                }
            }
            else
            {
                if (_random.Next(0, 2) == 0)
                {
                    continue;
                }

                var direction = (Direction)_random.Next(0, 4);

                var (wanderX, wanderY) = direction switch
                {
                    Direction.Up => (npc.X, npc.Y - 1),
                    Direction.Down => (npc.X, npc.Y + 1),
                    Direction.Left => (npc.X - 1, npc.Y),
                    Direction.Right => (npc.X + 1, npc.Y),
                    _ => (npc.X, npc.Y)
                };

                if (_map.IsNpcPassable(wanderX, wanderY))
                {
                    await MoveEntityAsync(npc.EntityId, direction);
                }
            }
        }
    }

    private async ValueTask ProcessNpcRespawnsAsync()
    {
        var now = DateTime.UtcNow;
        var count = _pendingRespawns.Count;

        for (var i = 0; i < count; i++)
        {
            if (!_pendingRespawns.TryDequeue(out var entry))
            {
                break;
            }

            if (entry.RespawnAt <= now)
            {
                await RespawnNpcAsync(entry.Npc);
            }
            else
            {
                _pendingRespawns.Enqueue(entry);
            }
        }
    }

    private async ValueTask RespawnNpcAsync(Npc npc)
    {
        var (x, y) = FindRandomPassableTile();

        npc.X = x;
        npc.Y = y;
        npc.Health = npc.MaxHealth;
        npc.Mana = npc.MaxMana;
        npc.TargetEntityId = null;
        npc.LastAttackTime = null;
        npc.LastMoveTime = null;
        npc.Direction = (Direction)_random.Next(0, 4);

        await AddEntityAsync(npc);
    }

    private async ValueTask RegenHealthAsync()
    {
        foreach (var entity in _entities.Values)
        {
            if (entity.Health <= 0 || entity.Health >= entity.MaxHealth)
            {
                continue;
            }

            var baseRegen = entity.MaxHealth / 20;
            var variance = 1.0 + (_random.NextDouble() * 0.3 - 0.15);
            var regenAmount = Math.Max(1, (int)(baseRegen * variance));

            entity.Health = Math.Min(entity.MaxHealth, entity.Health + regenAmount);

            await SendToAllAsync("entity_health", new EntityHealth
            {
                EntityId = entity.EntityId,
                Health = entity.Health,
                MaxHealth = entity.MaxHealth
            });
        }
    }

    private Direction? FindPathNextStep(int fromX, int fromY, int toX, int toY)
    {
        if (fromX == toX && fromY == toY) return null;

        var start = (fromX, fromY);
        var goal = (toX, toY);

        var openSet = new PriorityQueue<(int x, int y), int>();
        var cameFrom = new Dictionary<(int, int), ((int, int) parent, Direction dir)>();
        var gScore = new Dictionary<(int, int), int> { [start] = 0 };
        var closed = new HashSet<(int, int)>();

        openSet.Enqueue(start, Heuristic(fromX, fromY, toX, toY));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == goal)
            {
                return ReconstructFirstStep(cameFrom, start, goal);
            }

            if (!closed.Add(current)) continue;

            var (cx, cy) = current;

            foreach (var (neighbor, dir) in GetPassableNeighbors(cx, cy))
            {
                if (closed.Contains(neighbor)) continue;

                var tentativeG = gScore[current] + 1;

                if (gScore.TryGetValue(neighbor, out var existingG) && tentativeG >= existingG)
                {
                    continue;
                }

                cameFrom[neighbor] = (current, dir);

                gScore[neighbor] = tentativeG;

                var f = tentativeG + Heuristic(neighbor.x, neighbor.y, toX, toY);

                openSet.Enqueue(neighbor, f);
            }
        }

        return null;
    }

    private static int Heuristic(int x1, int y1, int x2, int y2)
    {
        return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
    }

    private IEnumerable<((int x, int y) pos, Direction dir)> GetPassableNeighbors(int x, int y)
    {
        if (_map.IsPassable(x, y - 1)) yield return ((x, y - 1), Direction.Up);
        if (_map.IsPassable(x, y + 1)) yield return ((x, y + 1), Direction.Down);
        if (_map.IsPassable(x - 1, y)) yield return ((x - 1, y), Direction.Left);
        if (_map.IsPassable(x + 1, y)) yield return ((x + 1, y), Direction.Right);
    }

    private static Direction ReconstructFirstStep(Dictionary<(int, int), ((int, int) parent, Direction dir)> cameFrom, (int, int) start, (int, int) goal)
    {
        var current = goal;

        while (true)
        {
            var (parent, dir) = cameFrom[current];
            if (parent == start)
            {
                return dir;
            }

            current = parent;
        }
    }

    private bool IsTileOccupied(IEntity mover, int x, int y)
    {
        return mover switch
        {
            Npc => _entities.Values
                .Any(e => e.EntityId != mover.EntityId && e.X == x && e.Y == y),

            Player => _entities.Values
                .Any(e => e.EntityId != mover.EntityId && e is Npc && e.X == x && e.Y == y),

            _ => false
        };
    }

    [LoggerMessage(LogLevel.Information, "Loaded map: '{MapPath}'")]
    partial void LogMapLoaded(string mapPath);

    [LoggerMessage(LogLevel.Information, "Entity '{Name}' (ID: {EntityId}) added to map '{MapPath}'")]
    partial void LogEntityAdded(string name, int entityId, string mapPath);

    [LoggerMessage(LogLevel.Information, "Entity '{Name}' (ID: {EntityId}) removed from map '{MapPath}'")]
    partial void LogEntityRemoved(string name, int entityId, string mapPath);

    [LoggerMessage(LogLevel.Information, "[Local] {CharacterName}: {Message}")]
    partial void LogChat(string characterName, string message);
}
