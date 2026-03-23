namespace GDMirage.Server.Configuration;

public sealed record ServerOptions
{
    public string ContentRoot { get; set; } = "Content";
    public string StartMap { get; set; } = "maps/test_map.tmj";
    public int StartX { get; set; } = 5;
    public int StartY { get; set; } = 5;
}
