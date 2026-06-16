using System.Security.Claims;
using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>
/// Autenticazione del Control Plane: registrazione self-service del cliente
/// (crea org + owner), login e logout. Login unico per SuperAdmin e TenantOwner,
/// con redirect in base al ruolo.
/// </summary>
[Route("Account")]
[EnableRateLimiting("auth")] // anti brute-force su login/registrazione
public sealed class AccountController : Controller
{
    private readonly ControlPlaneDbContext _db;
    private readonly Aski.ControlPlane.Services.Audit.IAuditLogger _audit;

    public AccountController(ControlPlaneDbContext db, Aski.ControlPlane.Services.Audit.IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public record RegisterForm(string CompanyName, string Email, string Password);
    public record LoginForm(string Email, string Password);

    // --- Registrazione (self-service, crea Tenant + TenantOwner) ---

    [HttpGet("Register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        ViewData["Title"] = "Crea il tuo account";
        return View();
    }

    [HttpPost("Register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.CompanyName) ||
            string.IsNullOrWhiteSpace(form.Email) ||
            string.IsNullOrWhiteSpace(form.Password) || form.Password.Length < 8)
        {
            ViewData["Error"] = "Compila tutti i campi (password minimo 8 caratteri).";
            return View();
        }

        var email = form.Email.Trim().ToLowerInvariant();
        if (await _db.PortalUsers.AnyAsync(u => u.Email == email, ct))
        {
            ViewData["Error"] = "Email già registrata. Prova ad accedere.";
            return View();
        }

        // Crea l'org del cliente e l'utente owner in un'unica transazione.
        var tenant = new Tenant
        {
            CompanyName = form.CompanyName.Trim(),
            BillingEmail = email,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var user = new PortalUser
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(form.Password),
            DisplayName = form.CompanyName.Trim(),
            Role = PortalUserRole.TenantOwner,
            TenantId = tenant.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.PortalUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        await SignInAsync(user);
        await _audit.LogAsync("auth.register", $"Tenant#{tenant.Id}", $"org={tenant.CompanyName}", ct);
        return RedirectToAction("Index", "Portal");
    }

    // --- Login ---

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["Title"] = "Accedi";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form, string? returnUrl, CancellationToken ct)
    {
        var email = (form.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.PortalUsers.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(form.Password, user.PasswordHash))
        {
            ViewData["Error"] = "Credenziali non valide.";
            ViewData["Title"] = "Accedi";
            return View();
        }

        await SignInAsync(user);
        await _audit.LogAsync("auth.login", $"User#{user.Id}", $"role={user.Role}", ct);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return user.Role == PortalUserRole.SuperAdmin
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Portal");
    }

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("Denied")]
    [AllowAnonymous]
    public IActionResult Denied()
    {
        ViewData["Title"] = "Accesso negato";
        return View();
    }

    private async Task SignInAsync(PortalUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.TenantId is not null)
            claims.Add(new Claim("tenantId", user.TenantId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}
