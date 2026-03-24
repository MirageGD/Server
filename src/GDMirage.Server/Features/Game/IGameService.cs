using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Entities;

namespace GDMirage.Server.Features.Game;

public interface IGameService
{
    Task<Player> CreatePlayerAsync(GameConnection connection, Character character);
    Task RemovePlayerAsync(int entityId);
    Player? GetPlayer(int entityId);
    IEnumerable<Player> GetAllPlayers();
    ValueTask SendToAllAsync<T>(string type, T payload);
    ValueTask GrantExperience(int entityId, int amount);
    Task RespawnPlayerAsync(Player player);
    Task WarpPlayerAsync(Player player);
}
