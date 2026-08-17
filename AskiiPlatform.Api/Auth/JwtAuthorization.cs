using System.Text;
using Askii.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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