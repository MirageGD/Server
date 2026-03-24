using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Entities;
using GDMirage.Server.Features.Game.Messages;
using GDMirage.Server.Features.Shared;

namespace GDMirage.Server.Features.Game;

public sealed partial class GameConnection(ILogger<GameConnection> logger, WebSocket webSocket, Character character)
{
    public Player? Player { get; set; }

    private static readonly UnboundedChannelOptions SendQueueOptions = new()
    {
        SingleReader = true,
        SingleWriter = false
    };

    private readonly Channel<byte[]> _sendQueue = Channel.CreateUnbounded<byte[]>(SendQueueOptions);

    public async Task RunAsync()
    {
        var buffer = new byte[1024 * 4];
        var sendTask = RunSendQueueAsync();

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;

                    case WebSocketMessageType.Text:
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        LogMessageReceived(logger, character.Name, message);

                        await HandleMessage(message);

                        break;

                    case WebSocketMessageType.Binary:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        finally
        {
            _sendQueue.Writer.Complete();

            await sendTask;
        }
    }

    private async Task RunSendQueueAsync()
    {
        await foreach (var data in _sendQueue.Reader.ReadAllAsync())
        {
            if (webSocket.State != WebSocketState.Open)
            {
                continue;
            }

            try
            {
                await webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                LogSendError(logger, character.Name, ex);
            }
        }
    }

    public ValueTask SendAsync<T>(string type, T payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(new { type, payload });

        return _sendQueue.Writer.WriteAsync(Encoding.UTF8.GetBytes(json), cancellationToken);
    }

    private async ValueTask HandleMessage(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);

            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeProperty))
            {
                return;
            }

            var messageType = typeProperty.GetString();

            switch (messageType)
            {
                case "move":
                    if (root.TryGetProperty("direction", out var moveDirectionProperty))
                    {
                        var directionStr = moveDirectionProperty.GetString();
                        if (Enum.TryParse<Direction>(directionStr, true, out var direction))
                        {
                            if (Player is not null)
                            {
                                await Player.CurrentMap.MoveEntityAsync(Player.EntityId, direction);
                            }
                        }
                    }

                    break;

                case "direction":
                    if (root.TryGetProperty("direction", out var faceDirectionProperty))
                    {
                        var directionStr = faceDirectionProperty.GetString();
                        if (Enum.TryParse<Direction>(directionStr, true, out var direction) && Player is not null)
                        {
                            await Player.CurrentMap.UpdateDirectionAsync(Player.EntityId, direction);
                        }
                    }

                    break;

                case "attack":
                    if (Player is not null)
                    {
                        await Player.CurrentMap.AttackAsync(Player.EntityId);
                    }

                    break;

                case "chat":
                    if (root.TryGetProperty("message", out var chatMessageProperty) && Player is not null)
                    {
                        var chatText = chatMessageProperty.GetString();
                        if (!string.IsNullOrEmpty(chatText))
                        {
                            await HandleChatMessage(Player, chatText);
                        }
                    }

                    break;

                case "warp":
                    if (Player is not null)
                    {
                        await Player.CurrentMap.WarpAsync(Player.EntityId);
                    }

                    break;
            }
        }
        catch (JsonException ex)
        {
            LogJsonError(logger, character.Name, ex);
        }
    }

    private async ValueTask HandleChatMessage(Player player, string text)
    {
        if (text.StartsWith('/'))
        {
            var response = new ChatMessage
            {
                Channel = "system",
                Message = $"Unknown command: {text}",
                Color = "#ff4444"
            };

            await SendAsync("chat", response);
        }
        else
        {
            await player.CurrentMap.BroadcastLocalChatAsync(player, text);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Received message from '{CharacterName}': {Message}")]
    static partial void LogMessageReceived(ILogger logger, string characterName, string message);

    [LoggerMessage(LogLevel.Error, "Error sending message for '{CharacterName}'")]
    static partial void LogSendError(ILogger logger, string characterName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error parsing JSON message from '{CharacterName}'")]
    static partial void LogJsonError(ILogger logger, string characterName, Exception exception);
}
