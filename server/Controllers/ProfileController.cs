using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using XayouGram.Backend.Data;
using XayouGram.Backend.DTOs;
using XayouGram.Backend.Services;

namespace XayouGram.Backend.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db)
    {
        _db = db;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPut("update")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(request.DisplayName))
            user.DisplayName = request.DisplayName;
        if (!string.IsNullOrEmpty(request.Bio))
            user.Bio = request.Bio;
        if (!string.IsNullOrEmpty(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        await _db.SaveChangesAsync();
        return Ok(AuthService.MapToDto(user));
    }

    [HttpPost("avatar")]
    public async Task<ActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var userId = GetUserId();
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExts.Contains(ext))
            return BadRequest(new { message = "Invalid file type. Use: jpg, png, gif, webp" });

        var fileName = $"avatar_{userId}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/avatars/{fileName}";

        // Update user avatar in database
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.AvatarUrl = url;
            await _db.SaveChangesAsync();
        }

        return Ok(new { url });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        return Ok(AuthService.MapToDto(user));
    }
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}