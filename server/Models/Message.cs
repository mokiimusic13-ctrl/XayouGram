namespace XayouGram.Backend.Models;

public enum MessageType
{
    Text,
    Image,
    Voice,
    Sticker,
    Document
}

public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public MessageType Type { get; set; } = MessageType.Text;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? EncryptedContent { get; set; }
    public int? ReplyToId { get; set; }
    public Message? ReplyTo { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
}