using Aski.Ticketing.Api.Auth;
using Aski.Ticketing.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Ticketing.Api.Controllers;

/// <summary>Autenticazione dell'istanza: login con email/password, rilascio JWT.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly TicketingDbContext _db;
    private readonly JwtTokenService _tokens;

    public AuthController(TicketingDbContext db, JwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string Token, string Role);

    [HttpPost("login")]
    [AllowAnonymous] // Unico endpoint pubblico: emette il JWT dopo verifica credenziali.
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive, ct);
        // Verifica sempre l'hash (anche se l'utente non esiste si evita timing leak grossolano).
        var ok = user is not null && BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!ok) return Unauthorized("Credenziali non valide");

        return Ok(new LoginResponse(_tokens.CreateToken(user!), user!.Role.ToString()));
    }
}
