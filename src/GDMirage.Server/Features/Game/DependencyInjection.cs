using GDMirage.Server.Features.Game.Entities;

namespace GDMirage.Server.Features.Game;

public static class DependencyInjection
{
    public static void AddGameServices(this IServiceCollection services)
    {
        services.AddSingleton<EntityIdGenerator>();
        services.AddSingleton<NpcInfoManager>();
        services.AddSingleton<ItemInfoManager>();
        services.AddSingleton<IGameMapManager, GameMapManager>();
        services.AddHostedService(sp => sp.GetRequiredService<IGameMapManager>());
        services.AddSingleton<IGameService, GameService>();
    }
}
