using System.Text.Json.Serialization;

namespace GDMirage.Server.Features.Game.Messages;

public sealed record ChatMessage
{
    [JsonPropertyName("channel")]
    public ChatChannel Channel { get; set; }

    [JsonPropertyName("entity_id")]
    public int? EntityId { get; set; }

    [JsonPropertyName("sender_name")]
    public string? SenderName { get; set; }

    [JsonPropertyName("sender_color")]
    public string? SenderColor { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#ffffff";
}
