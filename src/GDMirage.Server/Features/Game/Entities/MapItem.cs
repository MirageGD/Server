namespace GDMirage.Server.Features.Game.Entities;

public sealed class MapItem(int instanceId, ItemInfo info, int x, int y)
{
    public int InstanceId { get; } = instanceId;
    public ItemInfo Info { get; } = info;
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
}
