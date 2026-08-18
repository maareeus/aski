using Askii.Common.Extensions;

namespace Askii.Tests.Common;

public class EmailStringExtensionsTests
{
    [Theory]
    [InlineData("MARIO@EXAMPLE.COM", "mario@example.com")]
    [InlineData("  mario@example.com  ", "mario@example.com")]
    [InlineData("Mario.Rossi@Example.IT", "mario.rossi@example.it")]
    [InlineData("mario@example.com", "mario@example.com")]
    public void NormalizeEmail_abbassa_e_taglia_gli_spazi(string input, string expected)
        => Assert.Equal(expected, input.NormalizeEmail());

    [Fact]
    public void NormalizeEmail_su_stringa_vuota_resta_vuota()
        => Assert.Equal(string.Empty, "".NormalizeEmail());

    [Theory]
    [InlineData("mario@example.com")]
    [InlineData("mario.rossi+tag@sub.example.co.uk")]
    public void IsValidEmail_accetta_indirizzi_validi(string email)
        => Assert.True(email.IsValidEmail());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("senza-chiocciola")]
    [InlineData("@example.com")]
    [InlineData("mario@")]
    // MailAddress da solo accetterebbe queste forme, estraendo un display name
    // o un indirizzo diverso dal testo ricevuto.
    [InlineData("Mario Rossi <mario@example.com>")]
    [InlineData("<mario@example.com>")]
    [InlineData("\"Mario\" <mario@example.com>")]
    // Dominio senza punto: sintatticamente ammesso, di fatto non raggiungibile.
    [InlineData("mario@localhost")]
    public void IsValidEmail_rifiuta_indirizzi_non_validi(string? email)
        => Assert.False(email.IsValidEmail());

    [Fact]
    public void NormalizeEmail_e_indipendente_dalla_culture()
    {
        var originale = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            var turca = "MARIO@EXAMPLE.COM".NormalizeEmail();

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Con ToLower la I turca diventerebbe 'ı': con ToLowerInvariant no.
            Assert.Equal("mario@example.com", turca);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originale;
        }
    }
}
