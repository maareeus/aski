using Askii.Common.Security;
using Askii.Database.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace Askii.Tests.Common;

public class SecretProtectorTests
{
    private static ISecretProtector Protector(string applicazione = "test")
        => new DataProtectionSecretProtector(
            DataProtectionProvider.Create(applicazione));

    [Fact]
    public void Protect_e_Unprotect_sono_inversi()
    {
        var p = Protector();

        Assert.Equal("segretissima", p.Unprotect(p.Protect("segretissima")));
    }

    [Fact]
    public void Il_valore_cifrato_non_contiene_quello_in_chiaro()
    {
        var cifrato = Protector().Protect("segretissima");

        Assert.DoesNotContain("segretissima", cifrato);
        Assert.StartsWith("enc:v1:", cifrato);
    }

    [Fact]
    public void Due_cifrature_dello_stesso_valore_sono_diverse()
    {
        var p = Protector();

        // Data Protection usa un IV casuale: valori identici non producono lo
        // stesso testo cifrato, quindi non si possono confrontare fra loro.
        Assert.NotEqual(p.Protect("stessa"), p.Protect("stessa"));
    }

    [Fact]
    public void Un_valore_in_chiaro_legacy_viene_restituito_com_e()
    {
        // Le righe scritte prima della cifratura non hanno il prefisso: vanno
        // lette senza errori, altrimenti l'introduzione della cifratura avrebbe
        // rotto le configurazioni esistenti.
        Assert.Equal("vecchia-in-chiaro", Protector().Unprotect("vecchia-in-chiaro"));
    }

    [Fact]
    public void Un_valore_cifrato_con_altre_chiavi_diventa_vuoto()
    {
        var cifrato = Protector("applicazione-A").Protect("segretissima");

        // Anello di chiavi diverso: il valore non è recuperabile e l'opzione
        // risulta non configurata, che è la lettura corretta.
        Assert.Equal(string.Empty, Protector("applicazione-B").Unprotect(cifrato));
    }

    [Theory]
    [InlineData("")]
    public void I_valori_vuoti_passano_indenni(string valore)
    {
        var p = Protector();

        Assert.Equal(valore, p.Protect(valore));
        Assert.Equal(valore, p.Unprotect(valore));
    }

    [Fact]
    public void Solo_la_password_SMTP_e_considerata_segreta()
    {
        Assert.True(OpzioniSegrete.Contiene(Option.Email.SMTP_PASS));
        Assert.True(OpzioniSegrete.Contiene("SMTP_PASSWORD")); // confronto case-insensitive

        Assert.False(OpzioniSegrete.Contiene(Option.Email.SMTP_HOST));
        Assert.False(OpzioniSegrete.Contiene(Option.Email.SMTP_USER));
        Assert.False(OpzioniSegrete.Contiene(Option.Email.SMTP_PORT));
    }
}
