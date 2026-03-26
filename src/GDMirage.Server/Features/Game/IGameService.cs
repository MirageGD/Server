using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Entities;

namespace GDMirage.Server.Features.Game;

public interface IGameService
{
    Task<Player> CreatePlayerAsync(GameConnection connection, Account account, Character character);
    Task RemovePlayerAsync(int entityId);
    ValueTask GrantExperience(int entityId, int amount);
    Task RespawnPlayerAsync(Player player);
    Task WarpPlayerAsync(Player player);
    bool IsPlayerConnected(string accountName, string characterName);
}
