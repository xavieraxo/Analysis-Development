namespace Gateway.Blazor.Models;

public class ChatMessage
{
    public string ConversationId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset At { get; set; }
}

public class ChatResponse
{
    public ChatMessage? Message { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
}

