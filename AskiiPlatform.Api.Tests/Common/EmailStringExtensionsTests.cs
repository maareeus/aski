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
    public void IsValidEmail_rifiuta_indirizzi_non_validi(string? email)
        => Assert.False(email.IsValidEmail());
}
