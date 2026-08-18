using System.IdentityModel.Tokens.Jwt;
using Askii.Common;
using Askii.Database.Entities;
using Askii.Features.Auth;
using Askii.Tests.Infrastructure;

namespace Askii.Tests.Database;

/// <summary>
/// Il SecurityStamp è il meccanismo che rende revocabili i token: viene messo nel
/// JWT e confrontato a ogni richiesta autenticata. Qui si verifica che cambi
/// esattamente quando deve.
/// </summary>
public class SecurityStampTests
{
    private static User Nuovo(string ruolo = Roles.Client) =>
        User.Create("mario@example.com", "Password123!", "Mario", "Rossi", ruolo);

    [Fact]
    public void Ogni_utente_nasce_con_un_proprio_stamp()
    {
        var a = Nuovo();
        var b = Nuovo();

        Assert.NotEmpty(a.SecurityStamp);
        Assert.NotEqual(a.SecurityStamp, b.SecurityStamp);
    }

    [Fact]
    public void Il_cambio_password_invalida_i_token_precedenti()
    {
        var user = Nuovo();
        var prima = user.SecurityStamp;

        user.SetPassword("NuovaPassword456!");

        Assert.NotEqual(prima, user.SecurityStamp);
    }

    [Fact]
    public void Il_cambio_ruolo_invalida_i_token_precedenti()
    {
        var user = Nuovo();
        var prima = user.SecurityStamp;

        // Il ruolo è dentro il token: senza revoca un declassamento resterebbe
        // senza effetto fino alla scadenza naturale.
        user.UpdateRole(Roles.Operator);

        Assert.NotEqual(prima, user.SecurityStamp);
    }

    [Fact]
    public void RevokeSessions_cambia_lo_stamp()
    {
        var user = Nuovo();
        var prima = user.SecurityStamp;

        user.RevokeSessions();

        Assert.NotEqual(prima, user.SecurityStamp);
    }

    [Fact]
    public void L_attivazione_imposta_la_password_e_quindi_revoca()
    {
        var user = Nuovo();
        user.IsActive = false;
        var codice = user.IssueActivationCode();
        var prima = user.SecurityStamp;

        Assert.True(user.TryActivate(codice, "PasswordScelta123!"));

        Assert.NotEqual(prima, user.SecurityStamp);
    }

    [Fact]
    public void Modificare_l_anagrafica_non_revoca_le_sessioni()
    {
        var user = Nuovo();
        var prima = user.SecurityStamp;

        user.Name = "Marco";
        user.LastName = "Bianchi";
        user.SetEmail("marco@example.com");

        // Nome, cognome ed email non sono usati per autorizzare: cacciare
        // l'utente dalle proprie sessioni sarebbe una molestia inutile.
        Assert.Equal(prima, user.SecurityStamp);
    }

    [Fact]
    public void Le_operazioni_2FA_non_revocano_le_sessioni()
    {
        var user = Nuovo();
        var prima = user.SecurityStamp;

        user.EnableEmailOtp();
        user.StartTotpEnrollment();
        user.DisableAllTfa();

        // La 2FA agisce al login successivo: le sessioni già aperte non erano
        // ottenute con credenziali diverse.
        Assert.Equal(prima, user.SecurityStamp);
    }

    // --- presenza nel token ---

    [Fact]
    public void Il_token_di_accesso_porta_lo_stamp()
    {
        var user = Nuovo();

        var jwt = new JwtSecurityTokenHandler()
            .ReadJwtToken(TestFactory.TokenService().GenerateToken(user));

        var claim = jwt.Claims.SingleOrDefault(c => c.Type == TokenService.ClaimStamp);
        Assert.NotNull(claim);
        Assert.Equal(user.SecurityStamp, claim.Value);
    }

    [Fact]
    public void Due_token_emessi_dopo_un_cambio_password_hanno_stamp_diversi()
    {
        var user = Nuovo();
        var service = TestFactory.TokenService();
        var handler = new JwtSecurityTokenHandler();

        var primo = handler.ReadJwtToken(service.GenerateToken(user))
            .Claims.Single(c => c.Type == TokenService.ClaimStamp).Value;

        user.SetPassword("NuovaPassword456!");

        var secondo = handler.ReadJwtToken(service.GenerateToken(user))
            .Claims.Single(c => c.Type == TokenService.ClaimStamp).Value;

        Assert.NotEqual(primo, secondo);
    }

    [Fact]
    public void La_sfida_2FA_non_porta_lo_stamp()
    {
        var user = Nuovo();

        var jwt = new JwtSecurityTokenHandler()
            .ReadJwtToken(TestFactory.TokenService().GenerateTfaChallenge(user));

        // La sfida non apre endpoint, quindi non partecipa al confronto.
        Assert.DoesNotContain(jwt.Claims, c => c.Type == TokenService.ClaimStamp);
    }
}
