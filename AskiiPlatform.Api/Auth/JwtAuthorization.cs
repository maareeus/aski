using System.Security.Claims;
using System.Text;
using Askii.Common;
using Askii.Common.Extensions;
using Askii.Database;
using Askii.Features.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Askii.Authorization;

public static class JwtAuthorization
{
    public static class PolicyLevel
    {
        public static readonly string AdminPolicy = "AdminPolicy";
        public static readonly string OperatorPolicy = "OperatorPolicy";
        public static readonly string UserPolicy = "UserPolicy";
    }
    public static void Init(
        this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        builder.Services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
             o.TokenValidationParameters = new TokenValidationParameters
             {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"], 
                ValidAudience = config["Jwt:Audience"], 
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(config["Jwt:Key"]!))  
             };

             // La firma valida dice solo che il token è nostro e non scaduto, non
             // che è ancora buono: qui si controlla che l'utente esista, sia
             // ancora attivo e non abbia cambiato password o ruolo dopo
             // l'emissione. Costa una query per richiesta autenticata, prezzo
             // accettabile per avere una revoca immediata su un token da 8 ore.
             o.Events = new JwtBearerEvents
             {
                OnTokenValidated = async contesto =>
                {
                    var principal = contesto.Principal;
                    if (principal is null) { contesto.Fail("Token senza identità"); return; }

                    var stamp = principal.FindFirst(TokenService.ClaimStamp)?.Value;
                    if (string.IsNullOrEmpty(stamp)) { contesto.Fail("Token privo di impronta"); return; }

                    Guid userId;
                    try { userId = principal.CurrentUserId(); }
                    catch (Exception) { contesto.Fail("Token senza identificativo utente"); return; }

                    var db = contesto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                    var stato = await db.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.SecurityStamp, u.IsActive })
                        .SingleOrDefaultAsync();

                    if (stato is null) { contesto.Fail("Utente non più esistente"); return; }
                    if (!stato.IsActive) { contesto.Fail("Utente disattivato"); return; }
                    if (stato.SecurityStamp != stamp) { contesto.Fail("Credenziali cambiate dopo l'emissione del token"); return; }
                }
             };
        });
        builder.Services.AddAuthorization(JwtAuthorizationOptions);
    }

    private static void JwtAuthorizationOptions(AuthorizationOptions o)
    {
        o.AddPolicy(JwtAuthorization.PolicyLevel.AdminPolicy, p => p.RequireRole(Roles.Admin));
        o.AddPolicy(JwtAuthorization.PolicyLevel.OperatorPolicy, p => p.RequireRole(Roles.Admin, Roles.Operator));
        o.AddPolicy(JwtAuthorization.PolicyLevel.UserPolicy, p => p.RequireAuthenticatedUser());
    }
}