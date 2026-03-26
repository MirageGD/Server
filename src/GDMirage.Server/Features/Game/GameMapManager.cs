using System.Collections.Concurrent;
using System.Text.Json;
using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Game.Entities;
using GDMirage.Server.Features.Game.Maps;
using Microsoft.Extensions.Options;

namespace GDMirage.Server.Features.Game;

public sealed partial class GameMapManager(
    ILogger<GameMapManager> logger,
    IOptions<ServerOptions> options,
    EntityIdGenerator entityIdGenerator,
    InfoManager<NpcInfo> npcInfoManager,
    InfoManager<ItemInfo> itemInfoManager,
    IServiceProvider serviceProvider)
    : BackgroundService, IGameMapManager
{
    private readonly ConcurrentDictionary<string, GameMap> _maps = new();
    private readonly string _contentPath = options.Value.ContentRoot;

    public GameMap GetMap(string path)
    {
        return _maps.GetOrAdd(path, LoadMap);
    }

    private GameMap LoadMap(string mapPath)
    {
        var path = Path.Combine(_contentPath, mapPath);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Map file not found: {mapPath}", mapPath);
        }

        LogLoading(mapPath);

        var gameService = serviceProvider.GetRequiredService<IGameService>();
        var npcInfos = LoadNpcInfos(mapPath);

        return new GameMap(logger, gameService, mapPath, path, entityIdGenerator.GetNext, npcInfos, itemInfoManager);
    }

    private List<NpcInfo> LoadNpcInfos(string mapPath)
    {
        var path = Path.Combine(_contentPath, Path.ChangeExtension(mapPath, ".json"));

        if (!File.Exists(path))
        {
            return [];
        }

        var mapInfoJson = File.ReadAllText(path);
        var mapInfo = JsonSerializer.Deserialize<MapInfo>(mapInfoJson);

        var npcs = mapInfo?.Npcs;
        if (npcs is null)
        {
            return [];
        }

        return npcs.Select(npcInfoManager.Get).ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var tasks = _maps.Values.Select(map => map.UpdateAsync());

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in map update");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    [LoggerMessage(LogLevel.Information, "Loading map: {MapPath}")]
    partial void LogLoading(string mapPath);
}
