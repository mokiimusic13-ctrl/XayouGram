using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace XayouGram.Backend.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost("avatar")]
    public async Task<ActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var userId = GetUserId();
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"avatar_{userId}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/avatars/{fileName}";
        return Ok(new { url });
    }

    [HttpPost("voice")]
    public async Task<ActionResult> UploadVoice(IFormFile audio)
    {
        if (audio == null || audio.Length == 0)
            return BadRequest(new { message = "No audio provided" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "voice");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(audio.FileName);
        var fileName = $"voice_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await audio.CopyToAsync(stream);
        }

        var url = $"/uploads/voice/{fileName}";
        return Ok(new { url });
    }

    [HttpPost("photo")]
    public async Task<ActionResult> UploadPhoto(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "photos");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"photo_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/photos/{fileName}";
        return Ok(new { url });
    }
}