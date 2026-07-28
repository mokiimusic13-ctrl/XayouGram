using Microsoft.AspNetCore.Mvc;
using XayouGram.Backend.DTOs;
using XayouGram.Backend.Services;

namespace XayouGram.Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await _authService.Register(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await _authService.Login(request);
        if (!result.Success)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpGet("user/{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _authService.GetUserById(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    [HttpGet("user/by-username/{username}")]
    public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
    {
        var user = await _authService.GetUserByUsername(username);
        if (user == null)
            return NotFound();
        return Ok(user);
    }
}