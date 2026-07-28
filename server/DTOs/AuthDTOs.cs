namespace XayouGram.Backend.DTOs;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
    public bool IsAdmin { get; set; }
    public string Role { get; set; } = "user";
    public int Stars { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChatDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public string? InviteLink { get; set; }
    public int? OwnerId { get; set; }
    public int MemberCount { get; set; }
    public bool IsPublic { get; set; }
    public MessageDto? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MessageDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public int? ReplyToId { get; set; }
    public string? ReplyContent { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public int? ReplyToId { get; set; }
    public string Type { get; set; } = "text";
}

public class CreateGroupRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Members { get; set; } = new();
}

public class CreateChannelRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class AdminActionRequest
{
    public string Action { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int Stars { get; set; }
}

public class StatsResponse
{
    public int TotalUsers { get; set; }
    public int OnlineUsers { get; set; }
    public int TotalChats { get; set; }
    public int TotalMessages { get; set; }
    public int TotalGroups { get; set; }
    public int TotalChannels { get; set; }
    public List<OnlineUserDto> OnlineUserList { get; set; } = new();
    public List<UserDto> RecentUsers { get; set; } = new();
}

public class OnlineUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}