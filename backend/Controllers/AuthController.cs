using System.Security.Claims;
using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Autenticazione: login (JWT access + refresh), refresh con rotazione, logout, profilo.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokens;

    public AuthController(UserManager<AppUser> users, AppDbContext db, JwtTokenService tokens)
    {
        _users = users;
        _db = db;
        _tokens = tokens;
    }

    /// <summary>Login con email/password. Restituisce access token JWT e refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive || !await _users.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { error = "Credenziali non valide." });

        return await IssueAsync(user, ct);
    }

    /// <summary>Rinnova l'access token usando un refresh token valido (con rotazione).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req, CancellationToken ct)
    {
        var token = await _db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == req.RefreshToken, ct);

        if (token is null || !token.IsActive || token.User is null || !token.User.IsActive)
            return Unauthorized(new { error = "Refresh token non valido o scaduto." });

        // Rotazione: revoca quello usato ed emette nuovi token.
        token.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await IssueAsync(token.User, ct);
    }

    /// <summary>Revoca il refresh token corrente (logout).</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshRequest req, CancellationToken ct)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == req.RefreshToken, ct);
        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    /// <summary>Profilo dell'utente autenticato.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var roles = await _users.GetRolesAsync(user);
        return new UserInfo(user.Id, user.Email!, user.FullName, user.CompanyId, roles.ToList());
    }

    /// <summary>Cambio password dell'utente autenticato.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    private async Task<AuthResponse> IssueAsync(AppUser user, CancellationToken ct)
    {
        var roles = await _users.GetRolesAsync(user);
        var access = _tokens.CreateAccessToken(user, roles);
        var refresh = _tokens.CreateRefreshToken(user.Id);

        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        var info = new UserInfo(user.Id, user.Email!, user.FullName, user.CompanyId, roles.ToList());
        return new AuthResponse(access, refresh.Token, _tokens.AccessTokenLifetimeSeconds, info);
    }
}
