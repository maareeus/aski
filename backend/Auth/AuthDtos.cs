namespace Aski.Tickets.Api.Auth;

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Risposta di autenticazione: token + profilo essenziale.</summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    UserInfo User);

public record UserInfo(
    string Id,
    string Email,
    string? FullName,
    int? CompanyId,
    IReadOnlyList<string> Roles);
