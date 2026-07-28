using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using XayouGram.Backend.Data;
using XayouGram.Backend.DTOs;
using XayouGram.Backend.Models;

namespace XayouGram.Backend.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return new AuthResponse { Success = false, Message = "Username already taken" };

        if (await _db.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber))
            return new AuthResponse { Success = false, Message = "Phone number already registered" };

        var user = new User
        {
            Username = request.Username,
            PhoneNumber = request.PhoneNumber,
            DisplayName = string.IsNullOrEmpty(request.DisplayName) ? request.Username : request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return new AuthResponse
        {
            Success = true,
            Token = token,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new AuthResponse { Success = false, Message = "Invalid credentials" };

        var token = GenerateJwtToken(user);
        return new AuthResponse
        {
            Success = true,
            Token = token,
            User = MapToDto(user)
        };
    }

    public async Task<UserDto?> GetUserById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetUserByUsername(string username)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user == null ? null : MapToDto(user);
    }

public async Task UpdateLastSeen(int userId)
{
    var user = await _db.Users.FindAsync(userId);
    if (user != null)
    {
        user.LastSeen = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "XayouGramSuperSecretKey2024!@#$%^&*()"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: "XayouGram",
            audience: "XayouGramUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            PhoneNumber = user.PhoneNumber,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            IsVerified = user.IsVerified,
            IsAdmin = user.IsAdmin,
            Role = user.Role,
            Stars = user.Stars,
            IsOnline = user.IsOnline,
            LastSeen = user.LastSeen,
            CreatedAt = user.CreatedAt
        };
    }
}