using System.Collections.Concurrent;
using System.Text.Json;
using GDMirage.Server.Configuration;
using Microsoft.Extensions.Options;

namespace GDMirage.Server.Features.Game.Entities;

public sealed partial class InfoManager<T>(ILogger<InfoManager<T>> logger, IOptions<ServerOptions> options) where T : Info
{
    private static readonly string TypeName = typeof(T).Name.Replace("Info", "");
    private static readonly string TypeDirectory = typeof(T).Name.Replace("Info", "").ToLowerInvariant() + "s";

    private readonly ConcurrentDictionary<string, T> _cache = new();
    private readonly string _contentRoot = options.Value.ContentRoot;

    public T Get(string id)
    {
        return _cache.GetOrAdd(id, LoadInfo);
    }

    private T LoadInfo(string id)
    {
        var path = Path.Combine(_contentRoot, TypeDirectory, $"{id}.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Item info file not found: {path}", path);
        }

        var infoJson = File.ReadAllText(path);
        var info = JsonSerializer.Deserialize<T>(infoJson) ?? throw new InvalidOperationException(
            $"Failed to deserialize item info from: {path}");

        LogLoaded(id, TypeName, info.Name);

        return info with { Id = id };
    }

    [LoggerMessage(LogLevel.Information, "Loaded {TypeName} info '{ItemName}' from '{Id}'")]
    private partial void LogLoaded(string id, string typeName, string itemName);
}
