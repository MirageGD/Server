using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GDMirage.Server.Features.Game.Maps.Objects;

namespace GDMirage.Server.Features.Game.Maps;

public sealed class Map
{
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private readonly TileType[,] _tiles;
    private readonly List<MapObject> _objects = [];

    public int Width { get; }
    public int Height { get; }

    public Map(string path)
    {
        var json = File.ReadAllText(path);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Width = root.GetProperty("width").GetInt32();
        Height = root.GetProperty("height").GetInt32();
        _tileWidth = root.GetProperty("tilewidth").GetInt32();
        _tileHeight = root.GetProperty("tileheight").GetInt32();

        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentException("Invalid map dimensions");
        }

        _tiles = new TileType[Width, Height];

        var firstGids = GetTilesetFirstGids(root.GetProperty("tilesets"));
        var layers = root.GetProperty("layers").EnumerateArray().ToArray();

        ParseMetaLayers(layers, firstGids);
        ParseObjectGroups(layers);
    }

    private static List<int> GetTilesetFirstGids(JsonElement tilesets)
    {
        return tilesets.EnumerateArray()
            .Select(tileset => tileset
                .GetProperty("firstgid").GetInt32())
            .OrderByDescending(gid => gid)
            .ToList();
    }

    private void ParseObjectGroups(JsonElement[] layers)
    {
        var objectGroups = layers
            .Where(layer => layer
                .GetProperty("type")
                .GetString() == "objectgroup");

        foreach (var objectGroup in objectGroups)
        {
            ParseObjectGroup(objectGroup);
        }
    }

    private void ParseObjectGroup(JsonElement objectGroup)
    {
        var objects = objectGroup.GetProperty("objects").EnumerateArray();

        foreach (var obj in objects)
        {
            ParseObject(obj);
        }
    }


    private void ParseObject(JsonElement obj)
    {
        var type = obj.GetProperty("type").GetString();
        var properties = obj.GetProperty("properties")
            .EnumerateArray()
            .ToArray();

        var x = obj.GetProperty("x").GetInt32() / _tileWidth;
        var y = obj.GetProperty("y").GetInt32() / _tileHeight;
        var width = obj.GetProperty("width").GetInt32() / _tileWidth;
        var height = obj.GetProperty("height").GetInt32() / _tileWidth;

        switch (type)
        {
            case "warp":
                ParseWarp(x, y, width, height, properties);
                break;
        }
    }

    private void ParseWarp(int x, int y, int width, int height, JsonElement[] properties)
    {
        var targetDirection = GetProperty(properties, "target_direction")?.GetProperty("value");
        
        _objects.Add(new Warp
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            TargetMap = GetPropertyString(properties, "target_map"),
            TargetX = GetPropertyInt(properties, "target_x", 0),
            TargetY = GetPropertyInt(properties, "target_y", 0),
            TargetDirection = targetDirection?.GetString()
        });
    }

    private static string GetPropertyString(JsonElement[] properties, string name, string defaultValue = "")
    {
        var property = GetProperty(properties, name);
        if (property is null)
        {
            return defaultValue;
        }

        var type = property.Value.GetProperty("type").GetString();
        if (type is null || !type.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return defaultValue;
        }

        return property.Value.GetProperty("value").GetString() ?? defaultValue;
    }

    private static int GetPropertyInt(JsonElement[] properties, string name, int defaultValue)
    {
        var property = GetProperty(properties, name);
        if (property is null)
        {
            return defaultValue;
        }

        var type = property.Value.GetProperty("type").GetString();
        if (type is null || !type.Equals("int", StringComparison.OrdinalIgnoreCase))
        {
            return defaultValue;
        }

        return property.Value.GetProperty("value").TryGetInt32(out var value)
            ? value
            : defaultValue;
    }


    private static JsonElement? GetProperty(JsonElement[] properties, string name)
    {
        foreach (var property in properties)
        {
            var propertyName = property.GetProperty("name").GetString();
            if (propertyName is not null && propertyName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }
        }

        return null;
    }

    private void ParseMetaLayers(JsonElement[] layers, List<int> firstGids)
    {
        foreach (var layer in layers)
        {
            if (layer.GetProperty("type").GetString() != "tilelayer")
            {
                continue;
            }

            var layerName = layer.GetProperty("name").GetString();
            if (string.IsNullOrEmpty(layerName) || !layerName.StartsWith('@'))
            {
                continue;
            }

            ParseMetaLayer(layer, firstGids);
        }
    }

    private void ParseMetaLayer(JsonElement layer, List<int> firstGids)
    {
        var x = layer.GetProperty("x").GetInt32();
        var y = layer.GetProperty("y").GetInt32();
        var width = layer.GetProperty("width").GetInt32();
        var height = layer.GetProperty("height").GetInt32();

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var data = GetLayerData(layer);

        for (var i = 0; i < width * height; i++)
        {
            var tileX = x + i % width;
            var tileY = y + i / width;

            _tiles[tileX, tileY] = GetTileType(data[i], firstGids);
        }
    }

    private static TileType GetTileType(int gid, List<int> firstGids)
    {
        if (gid == 0)
        {
            return TileType.None;
        }

        return firstGids
            .Where(firstGid => gid >= firstGid)
            .Select(firstGid => (TileType)(1 + gid - firstGid))
            .FirstOrDefault();
    }

    private static int[] GetLayerData(JsonElement layer)
    {
        return layer.GetProperty("data")
            .EnumerateArray()
            .Select(e => e.GetInt32())
            .ToArray();
    }

    public bool IsPassable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return false;
        }

        return _tiles[x, y] != TileType.Blocked;
    }

    public bool IsNpcPassable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return false;
        }

        return _tiles[x, y] == TileType.None;
    }

    public bool TryGetWarpAt(int x, int y, [NotNullWhen(true)] out Warp? result)
    {
        var warp = _objects.OfType<Warp>().FirstOrDefault(w => w.HasPoint(x, y));

        result = warp;

        return warp != null;
    }
}
