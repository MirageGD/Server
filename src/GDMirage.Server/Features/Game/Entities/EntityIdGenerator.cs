namespace GDMirage.Server.Features.Game.Entities;

public sealed class EntityIdGenerator
{
    private int _nextId;

    public int GetNext() => Interlocked.Increment(ref _nextId);
}
