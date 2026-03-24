namespace GDMirage.Server.Features.Game.Maps.Objects;

public abstract record MapObject
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }

    public bool HasPoint(int x, int y)
    {
        return x >= X && x < X + Width && y >= Y && y < Y + Height;
    }
}
