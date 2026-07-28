namespace XayouGram.Backend.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
    public bool IsAdmin { get; set; }
    public string Role { get; set; } = "user";
    public int Stars { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
    
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChatMember> ChatMembers { get; set; } = new List<ChatMember>();
}