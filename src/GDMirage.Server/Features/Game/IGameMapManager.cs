namespace GDMirage.Server.Features.Game;

public interface IGameMapManager : IHostedService
{
    GameMap GetMap(string path);
}
