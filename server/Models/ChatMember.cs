namespace XayouGram.Backend.Models;

public enum MemberRole
{
    Member,
    Admin,
    Owner
}

public class ChatMember
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public MemberRole Role { get; set; } = MemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}