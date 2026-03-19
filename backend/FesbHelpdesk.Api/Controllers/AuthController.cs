using FesbHelpdesk.Api.Data;
using FesbHelpdesk.Api.DTOs;
using FesbHelpdesk.Api.Models;
using FesbHelpdesk.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FesbHelpdesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;

    public AuthController(AppDbContext db, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (!email.EndsWith("@fesb.hr", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Dozvoljene su samo @fesb.hr email adrese." });
        }

        var emailAlreadyTaken = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailAlreadyTaken)
        {
            return BadRequest(new { message = "Korisnik s ovom email adresom već postoji." });
        }

        var newUser = new User
        {
            Email = email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Student
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        return Ok(BuildAuthResponse(newUser));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            return Unauthorized(new { message = "Pogrešna email adresa ili lozinka." });
        }

        var passwordMatches = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordMatches)
        {
            return Unauthorized(new { message = "Pogrešna email adresa ili lozinka." });
        }

        return Ok(BuildAuthResponse(user));
    }

    private AuthResponseDto BuildAuthResponse(User user)
    {
        return new AuthResponseDto
        {
            Token = _jwt.GenerateToken(user),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            }
        };
    }
}
