using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Askii.Common;
using Askii.Database.Entities;
using Askii.Tests.Infrastructure;

namespace Askii.Tests.Features.Auth;

public class TokenServiceTests
{
    private static User Utente(string role = Roles.Operator)
    {
        var u = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", role);
        u.IsActive = true;
        return u;
    }

    private static JwtSecurityToken Leggi(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void GenerateToken_produce_un_jwt_a_tre_segmenti()
    {
        var token = TestFactory.TokenService().GenerateToken(Utente());

        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void Il_token_contiene_issuer_e_audience_di_configurazione()
    {
        var jwt = Leggi(TestFactory.TokenService().GenerateToken(Utente()));

        Assert.Equal(TestFactory.JwtIssuer, jwt.Issuer);
        Assert.Contains(TestFactory.JwtAudience, jwt.Audiences);
    }

    [Fact]
    public void Il_token_contiene_id_email_nome_e_ruolo()
    {
        var user = Utente(Roles.Admin);
        var jwt = Leggi(TestFactory.TokenService().GenerateToken(user));

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == "email").Value);
        Assert.Equal("Mario Rossi", jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(Roles.Admin, jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void Il_token_scade_dopo_otto_ore()
    {
        var jwt = Leggi(TestFactory.TokenService().GenerateToken(Utente()));

        var atteso = DateTime.UtcNow.AddHours(8);
        Assert.InRange(jwt.ValidTo, atteso.AddMinutes(-2), atteso.AddMinutes(2));
    }

    [Fact]
    public void Il_token_e_firmato_in_HS256()
    {
        var jwt = Leggi(TestFactory.TokenService().GenerateToken(Utente()));

        Assert.Equal("HS256", jwt.Header.Alg);
    }

    [Fact]
    public void Senza_chiave_jwt_solleva_InvalidOperationException()
    {
        var service = TestFactory.TokenService(key: null);

        var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateToken(Utente()));
        Assert.Equal("Missing chiave jwt", ex.Message);
    }

    [Fact]
    public void La_chiave_viene_letta_case_insensitive()
    {
        // TokenService legge "JWT:Key" mentre la config usa "Jwt:Key":
        // funziona solo perché le chiavi di IConfiguration sono case-insensitive.
        var service = TestFactory.TokenService();

        Assert.NotEmpty(service.GenerateToken(Utente()));
    }

    [Fact]
    public void Due_token_dello_stesso_utente_non_sono_distinguibili_non_avendo_jti()
    {
        var user = Utente();
        var service = TestFactory.TokenService();

        var jwt1 = Leggi(service.GenerateToken(user));
        var jwt2 = Leggi(service.GenerateToken(user));

        // Nessun claim "jti": i token non sono identificabili singolarmente,
        // quindi non sono revocabili puntualmente.
        Assert.DoesNotContain(jwt1.Claims, c => c.Type == "jti");
        Assert.DoesNotContain(jwt2.Claims, c => c.Type == "jti");
    }
}
