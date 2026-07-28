using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XayouGram.Backend.Data;
using XayouGram.Backend.DTOs;
using XayouGram.Backend.Models;
using XayouGram.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace XayouGram.Backend.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _authService;

    public ChatController(AppDbContext db, AuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("list")]
    public async Task<ActionResult<List<ChatDto>>> GetChats()
    {
        var userId = GetUserId();
        var chatIds = await _db.ChatMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ChatId)
            .ToListAsync();

        var chats = await _db.Chats
            .Where(c => chatIds.Contains(c.Id))
            .Include(c => c.Members)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .ToListAsync();

        var chatDtos = chats.Select(c => new ChatDto
        {
            Id = c.Id,
            Type = c.Type.ToString().ToLower(),
            Title = c.Type == ChatType.Private 
                ? c.Members.First(m => m.UserId != userId).User?.DisplayName ?? "Unknown"
                : c.Title ?? "Unknown",
            PhotoUrl = c.PhotoUrl,
            OwnerId = c.OwnerId,
            MemberCount = c.Members.Count,
            LastMessage = c.Messages.FirstOrDefault() != null ? new MessageDto
            {
                Id = c.Messages.First().Id,
                Content = c.Messages.First().Content,
                SenderId = c.Messages.First().SenderId,
                SentAt = c.Messages.First().SentAt,
                Type = c.Messages.First().Type.ToString().ToLower()
            } : null,
            CreatedAt = c.CreatedAt
        }).ToList();

        return Ok(chatDtos);
    }

    [HttpGet("{id}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(int id, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == id && cm.UserId == userId);
        if (!isMember) return Forbid();

        var messages = await _db.Messages
            .Where(m => m.ChatId == id && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Include(m => m.Sender)
            .Include(m => m.ReplyTo)
            .ToListAsync();

        var dtos = messages.OrderBy(m => m.SentAt).Select(m => new MessageDto
        {
            Id = m.Id,
            ChatId = m.ChatId,
            SenderId = m.SenderId,
            SenderUsername = m.Sender.Username,
            SenderDisplayName = m.Sender.DisplayName,
            SenderAvatar = m.Sender.AvatarUrl,
            Type = m.Type.ToString().ToLower(),
            Content = m.Content,
            MediaUrl = m.MediaUrl,
            ReplyToId = m.ReplyToId,
            ReplyContent = m.ReplyTo?.Content,
            SentAt = m.SentAt,
            EditedAt = m.EditedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id}/send")]
    public async Task<ActionResult<MessageDto>> SendMessage(int id, SendMessageRequest request)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == id && cm.UserId == userId);
        if (!isMember) return Forbid();

        var message = new Message
        {
            ChatId = id,
            SenderId = userId,
            Content = request.Content,
            MediaUrl = request.MediaUrl,
            ReplyToId = request.ReplyToId,
            Type = Enum.Parse<MessageType>(request.Type, true),
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await _db.Entry(message).Reference(m => m.Sender).LoadAsync();
        await _db.Entry(message).Reference(m => m.ReplyTo).LoadAsync();

        return Ok(new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderUsername = message.Sender.Username,
            SenderDisplayName = message.Sender.DisplayName,
            SenderAvatar = message.Sender.AvatarUrl,
            Type = message.Type.ToString().ToLower(),
            Content = message.Content,
            MediaUrl = message.MediaUrl,
            ReplyToId = message.ReplyToId,
            ReplyContent = message.ReplyTo?.Content,
            SentAt = message.SentAt
        });
    }

    [HttpPost("create-private/{userId2}")]
    public async Task<ActionResult<ChatDto>> CreatePrivateChat(int userId2)
    {
        var userId = GetUserId();
        
        var existingChat = await _db.ChatMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ChatId)
            .Intersect(_db.ChatMembers
                .Where(cm => cm.UserId == userId2)
                .Select(cm => cm.ChatId))
            .FirstOrDefaultAsync();

        if (existingChat != 0)
        {
            var chat = await _db.Chats.FindAsync(existingChat);
            return Ok(new ChatDto { Id = chat!.Id, Type = "private" });
        }

        var newChat = new Chat { Type = ChatType.Private, CreatedAt = DateTime.UtcNow };
        _db.Chats.Add(newChat);
        await _db.SaveChangesAsync();

        _db.ChatMembers.Add(new ChatMember { ChatId = newChat.Id, UserId = userId, Role = MemberRole.Member });
        _db.ChatMembers.Add(new ChatMember { ChatId = newChat.Id, UserId = userId2, Role = MemberRole.Member });
        await _db.SaveChangesAsync();

        return Ok(new ChatDto { Id = newChat.Id, Type = "private" });
    }

    [HttpPost("create-group")]
    public async Task<ActionResult<ChatDto>> CreateGroup(CreateGroupRequest request)
    {
        var userId = GetUserId();
        
        var chat = new Chat
        {
            Type = ChatType.Group,
            Title = request.Title,
            Description = request.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow,
            InviteLink = Guid.NewGuid().ToString("N")[..8]
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        _db.ChatMembers.Add(new ChatMember { ChatId = chat.Id, UserId = userId, Role = MemberRole.Owner });

        foreach (var username in request.Members)
        {
            var member = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (member != null)
                _db.ChatMembers.Add(new ChatMember { ChatId = chat.Id, UserId = member.Id, Role = MemberRole.Member });
        }

        await _db.SaveChangesAsync();

        return Ok(new ChatDto
        {
            Id = chat.Id,
            Type = "group",
            Title = chat.Title,
            Description = chat.Description,
            OwnerId = chat.OwnerId,
            MemberCount = chat.Members.Count + 1,
            InviteLink = chat.InviteLink,
            CreatedAt = chat.CreatedAt
        });
    }

    [HttpPost("create-channel")]
    public async Task<ActionResult<ChatDto>> CreateChannel(CreateChannelRequest request)
    {
        var userId = GetUserId();
        
        var chat = new Chat
        {
            Type = ChatType.Channel,
            Title = request.Title,
            Description = request.Description,
            OwnerId = userId,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow,
            InviteLink = Guid.NewGuid().ToString("N")[..8]
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        _db.ChatMembers.Add(new ChatMember { ChatId = chat.Id, UserId = userId, Role = MemberRole.Owner });
        await _db.SaveChangesAsync();

        return Ok(new ChatDto
        {
            Id = chat.Id,
            Type = "channel",
            Title = chat.Title,
            Description = chat.Description,
            OwnerId = chat.OwnerId,
            MemberCount = 1,
            InviteLink = chat.InviteLink,
            IsPublic = chat.IsPublic,
            CreatedAt = chat.CreatedAt
        });
    }

    [HttpPost("join/{inviteLink}")]
    public async Task<ActionResult> JoinByInvite(string inviteLink)
    {
        var userId = GetUserId();
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.InviteLink == inviteLink);
        if (chat == null) return NotFound("Chat not found");

        var alreadyMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == chat.Id && cm.UserId == userId);
        if (alreadyMember) return BadRequest("Already a member");

        _db.ChatMembers.Add(new ChatMember { ChatId = chat.Id, UserId = userId, Role = MemberRole.Member });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Joined successfully" });
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<UserDto>>> GetMembers(int id)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == id && cm.UserId == userId);
        if (!isMember) return Forbid();

        var members = await _db.ChatMembers
            .Where(cm => cm.ChatId == id)
            .Include(cm => cm.User)
            .Select(cm => AuthService.MapToDto(cm.User))
            .ToListAsync();

        return Ok(members);
    }
}