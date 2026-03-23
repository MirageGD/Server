using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game.Entities;

public static class EntityExtensions
{
    public static (int X, int Y) GetTileFacing(this IEntity entity)
    {
        return entity.Direction switch
        {
            Direction.Up => (entity.X, entity.Y - 1),
            Direction.Down => (entity.X, entity.Y + 1),
            Direction.Left => (entity.X - 1, entity.Y),
            Direction.Right => (entity.X + 1, entity.Y),
            _ => (entity.X, entity.Y)
        };
    }
}
