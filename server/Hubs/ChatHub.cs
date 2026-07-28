using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using XayouGram.Backend.Data;
using XayouGram.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace XayouGram.Backend.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    private int GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            var chatIds = await _db.ChatMembers
                .Where(cm => cm.UserId == userId)
                .Select(cm => cm.ChatId.ToString())
                .ToListAsync();

            foreach (var chatId in chatIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");

            await Groups.AddToGroupAsync(Context.ConnectionId, "online_users");
            await Clients.Others.SendAsync("UserOnline", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            await Clients.Others.SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(int chatId, string content, string type = "text", int? replyToId = null)
    {
        var userId = GetUserId();
        if (userId == 0) return;

        var isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
        if (!isMember) return;

        var message = new Message
        {
            ChatId = chatId,
            SenderId = userId,
            Content = content,
            Type = Enum.Parse<MessageType>(type, true),
            ReplyToId = replyToId,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await _db.Entry(message).Reference(m => m.Sender).LoadAsync();
        
        string replyContent = null!;
        if (replyToId.HasValue)
        {
            var reply = await _db.Messages.FindAsync(replyToId);
            replyContent = reply?.Content;
        }

        await Clients.Group($"chat_{chatId}").SendAsync("NewMessage", new
        {
            id = message.Id,
            chatId = message.ChatId,
            senderId = message.SenderId,
            senderUsername = message.Sender.Username,
            senderDisplayName = message.Sender.DisplayName,
            senderAvatar = message.Sender.AvatarUrl,
            type = message.Type.ToString().ToLower(),
            content = message.Content,
            replyToId = message.ReplyToId,
            replyContent,
            sentAt = message.SentAt
        });
    }

    public async Task SendTyping(int chatId)
    {
        var userId = GetUserId();
        if (userId == 0) return;

        await Clients.Group($"chat_{chatId}").SendAsync("UserTyping", new
        {
            chatId,
            userId,
            username = Context.User?.FindFirst(ClaimTypes.Name)?.Value
        });
    }

    public async Task JoinChat(int chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task LeaveChat(int chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }
}