using System.Collections.Concurrent;
using System.Text.Json;
using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Game.Entities;
using Microsoft.Extensions.Options;

namespace GDMirage.Server.Features.Game;

public sealed partial class ItemInfoManager(ILogger<ItemInfoManager> logger, IOptions<ServerOptions> options)
{
    private readonly ConcurrentDictionary<string, ItemInfo> _cache = new();
    private readonly string _contentRoot = options.Value.ContentRoot;

    public ItemInfo GetItemInfo(string id)
    {
        return _cache.GetOrAdd(id, LoadItemInfo);
    }

    private ItemInfo LoadItemInfo(string id)
    {
        var path = Path.Combine("items", $"{id}.json");
        var fullPath = Path.Combine(_contentRoot, path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Item info file not found: {path}", path);
        }

        var json = File.ReadAllText(fullPath);
        var itemInfo = JsonSerializer.Deserialize<ItemInfo>(json) ?? throw new InvalidOperationException(
            $"Failed to deserialize item info from: {path}");

        LogLoaded(id, itemInfo.Name);

        return itemInfo with { Id = id };
    }

    [LoggerMessage(LogLevel.Information, "Loaded item info '{ItemName}' from '{Id}'")]
    private partial void LogLoaded(string id, string itemName);
}
