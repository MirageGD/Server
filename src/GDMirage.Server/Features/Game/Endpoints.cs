using System.Net.WebSockets;
using GDMirage.Server.Features.Accounts.Services;

namespace GDMirage.Server.Features.Game;

public static partial class Endpoints
{
    public static void MapGameEndpoint(this WebApplication app)
    {
        var game = app.MapGroup("/api/v1/game").WithTags("Game");

        game.MapGet("/{token:guid}", HandleWebSocket);
    }

    private static async Task HandleWebSocket(
        Guid token, HttpContext context,
        IAccountService accountService,
        ICharacterService characterService,
        ICharacterTokenService characterTokenService,
        IGameService gameService)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var tokenInfo = characterTokenService.Exchange(token);
        if (tokenInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var accountName = tokenInfo.AccountName;
        var account = await accountService.GetAsync(accountName);
        var characterName = tokenInfo.CharacterName;
        var character = await characterService.GetAsync(characterName);
        if (account is null || character is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<GameConnection>();

        var connection = new GameConnection(logger, webSocket, character);
        var player = await gameService.CreatePlayerAsync(connection, account, character);

        logger.LogConnectionEstablished(accountName, characterName);

        try
        {
            await connection.RunAsync();
        }
        catch (WebSocketException ex)
        {
            if (ex.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
            {
                logger.LogConnectionError(accountName, characterName, ex);
            }
        }
        catch (Exception ex)
        {
            logger.LogConnectionError(accountName, characterName, ex);
        }
        finally
        {
            await gameService.RemovePlayerAsync(player.EntityId);
            await characterService.SaveAsync(character);

            logger.LogConnectionClosed(accountName, characterName);
        }
    }

    [LoggerMessage(LogLevel.Information, "WebSocket connection established for account '{AccountName}' with character '{CharacterName}'")]
    static partial void LogConnectionEstablished(this ILogger logger, string accountName, string characterName);

    [LoggerMessage(LogLevel.Error, "Error handling WebSocket connection for account '{AccountName}' with character '{CharacterName}'")]
    static partial void LogConnectionError(this ILogger logger, string accountName, string characterName, Exception exception);

    [LoggerMessage(LogLevel.Information, "WebSocket connection closed for account '{AccountName}' with character '{CharacterName}'")]
    static partial void LogConnectionClosed(this ILogger logger, string accountName, string characterName);
}
