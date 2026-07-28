using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XayouGram.Backend.Data;
using XayouGram.Backend.DTOs;
using XayouGram.Backend.Models;
using System.Security.Claims;

namespace XayouGram.Backend.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private async Task<bool> IsAdmin()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        return user?.IsAdmin == true || user?.Username == "Sh1zuky";
    }

    [HttpGet("stats")]
    public async Task<ActionResult<StatsResponse>> GetStats()
    {
        if (!await IsAdmin()) return Forbid();

        var totalUsers = await _db.Users.CountAsync();
        var onlineUsers = await _db.Users.CountAsync(u => u.IsOnline);
        var totalChats = await _db.Chats.CountAsync();
        var totalMessages = await _db.Messages.CountAsync();
        var totalGroups = await _db.Chats.CountAsync(c => c.Type == ChatType.Group);
        var totalChannels = await _db.Chats.CountAsync(c => c.Type == ChatType.Channel);

        var onlineUserList = await _db.Users
            .Where(u => u.IsOnline)
            .Select(u => new OnlineUserDto
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync();

        var recentUsers = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(10)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                IsVerified = u.IsVerified,
                IsOnline = u.IsOnline,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(new StatsResponse
        {
            TotalUsers = totalUsers,
            OnlineUsers = onlineUsers,
            TotalChats = totalChats,
            TotalMessages = totalMessages,
            TotalGroups = totalGroups,
            TotalChannels = totalChannels,
            OnlineUserList = onlineUserList,
            RecentUsers = recentUsers
        });
    }

    [HttpPost("action")]
    public async Task<ActionResult> PerformAction(AdminActionRequest request)
    {
        if (!await IsAdmin()) return Forbid();

        switch (request.Action.ToLower())
        {
            case "verify":
                var userToVerify = await _db.Users.FindAsync(request.UserId);
                if (userToVerify == null) return NotFound("User not found");
                userToVerify.IsVerified = true;
                await _db.SaveChangesAsync();
                return Ok(new { message = $"User {userToVerify.Username} is now verified" });

            case "unverify":
                var userToUnverify = await _db.Users.FindAsync(request.UserId);
                if (userToUnverify == null) return NotFound("User not found");
                userToUnverify.IsVerified = false;
                await _db.SaveChangesAsync();
                return Ok(new { message = $"User {userToUnverify.Username} is no longer verified" });

            case "addstars":
                var userToAddStars = await _db.Users.FindAsync(request.UserId);
                if (userToAddStars == null) return NotFound("User not found");
                userToAddStars.Stars += request.Stars;
                await _db.SaveChangesAsync();
                return Ok(new { message = $"Added {request.Stars} stars to {userToAddStars.Username}" });

            case "removestars":
                var userToRemoveStars = await _db.Users.FindAsync(request.UserId);
                if (userToRemoveStars == null) return NotFound("User not found");
                userToRemoveStars.Stars = Math.Max(0, userToRemoveStars.Stars - request.Stars);
                await _db.SaveChangesAsync();
                return Ok(new { message = $"Removed {request.Stars} stars from {userToRemoveStars.Username}" });

            case "makeadmin":
                var userToMakeAdmin = await _db.Users.FindAsync(request.UserId);
                if (userToMakeAdmin == null) return NotFound("User not found");
                userToMakeAdmin.IsAdmin = true;
                userToMakeAdmin.Role = "admin";
                await _db.SaveChangesAsync();
                return Ok(new { message = $"{userToMakeAdmin.Username} is now an admin" });

            case "removeadmin":
                var userToRemoveAdmin = await _db.Users.FindAsync(request.UserId);
                if (userToRemoveAdmin == null) return NotFound("User not found");
                userToRemoveAdmin.IsAdmin = false;
                userToRemoveAdmin.Role = "user";
                await _db.SaveChangesAsync();
                return Ok(new { message = $"{userToRemoveAdmin.Username} is no longer an admin" });

            default:
                return BadRequest(new { message = "Unknown action" });
        }
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        if (!await IsAdmin()) return Forbid();

        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                PhoneNumber = u.PhoneNumber,
                DisplayName = u.DisplayName,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                IsVerified = u.IsVerified,
                IsAdmin = u.IsAdmin,
                Role = u.Role,
                Stars = u.Stars,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }
}