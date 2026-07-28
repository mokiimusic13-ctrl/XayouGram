namespace XayouGram.Backend.Models;

public enum ChatType
{
    Private,
    Group,
    Channel
}

public class Chat
{
    public int Id { get; set; }
    public ChatType Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public string? InviteLink { get; set; }
    public int? OwnerId { get; set; }
    public User? Owner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublic { get; set; }
    
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
}