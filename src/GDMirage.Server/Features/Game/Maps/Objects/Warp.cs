namespace GDMirage.Server.Features.Game.Maps.Objects;

public sealed record Warp : MapObject
{
    public required string TargetMap { get; init; }
    public required int TargetX { get; init; }
    public required int TargetY { get; init; }
    public required string? TargetDirection { get; set; }
}
