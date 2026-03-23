using System.Collections.Concurrent;
using System.Text.Json;
using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Game.Entities;
using Microsoft.Extensions.Options;

namespace GDMirage.Server.Features.Game;

public sealed partial class NpcInfoManager(ILogger<NpcInfoManager> logger, IOptions<ServerOptions> options)
{
    private readonly ConcurrentDictionary<string, NpcInfo> _cache = new();
    private readonly string _contentRoot = options.Value.ContentRoot;

    public NpcInfo GetNpcInfo(string path)
    {
        return _cache.GetOrAdd(path, LoadNpcInfo);
    }

    private NpcInfo LoadNpcInfo(string path)
    {
        var fullPath = Path.Combine(_contentRoot, path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"NPC info file not found: {path}", path);
        }

        var json = File.ReadAllText(fullPath);
        var npcInfo = JsonSerializer.Deserialize<NpcInfo>(json) ?? throw new InvalidOperationException(
            $"Failed to deserialize NPC info from: {path}");

        LogLoaded(path, npcInfo.Name);

        return npcInfo;
    }

    [LoggerMessage(LogLevel.Information, "Loaded NPC info '{NpcName}' from '{Path}'")]
    private partial void LogLoaded(string path, string npcName);
}
