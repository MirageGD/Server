using System.Text.Json;

namespace GDMirage.Server.Features.Game.Maps;

public sealed class Map
{
    private readonly int _width;
    private readonly int _height;
    private readonly TileType[,] _tiles;

    public Map(string path)
    {
        var json = File.ReadAllText(path);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        _width = root.GetProperty("width").GetInt32();
        _height = root.GetProperty("height").GetInt32();

        if (_width <= 0 || _height <= 0)
        {
            throw new ArgumentException("Invalid map dimensions");
        }

        _tiles = new TileType[_width, _height];

        var firstGids = GetTilesetFirstGids(root.GetProperty("tilesets"));
        var layers = root.GetProperty("layers");

        ParseMetaLayers(layers, firstGids);
    }

    private static List<int> GetTilesetFirstGids(JsonElement tilesets)
    {
        return tilesets.EnumerateArray()
            .Select(tileset => tileset
                .GetProperty("firstgid").GetInt32())
            .OrderByDescending(gid => gid)
            .ToList();
    }

    private void ParseMetaLayers(JsonElement layers, List<int> firstGids)
    {
        foreach (var layer in layers.EnumerateArray())
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
        if (x < 0 || y < 0 || x >= _width || y >= _height)
        {
            return false;
        }

        return _tiles[x, y] != TileType.Blocked;
    }

    public bool IsNpcPassable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _width || y >= _height)
        {
            return false;
        }

        return _tiles[x, y] == TileType.None;
    }

    public int Width => _width;
    public int Height => _height;
}
