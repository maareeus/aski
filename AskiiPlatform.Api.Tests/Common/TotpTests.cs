using System.Text;
using Askii.Common.Security;

namespace Askii.Tests.Common;

public class Base32Tests
{
    [Theory]
    // Vettori di RFC 4648 §10.
    [InlineData("", "")]
    [InlineData("f", "MY")]
    [InlineData("fo", "MZXQ")]
    [InlineData("foo", "MZXW6")]
    [InlineData("foob", "MZXW6YQ")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI")]
    public void Encode_rispetta_i_vettori_di_RFC_4648(string testo, string atteso)
        => Assert.Equal(atteso, Base32.Encode(Encoding.ASCII.GetBytes(testo)));

    [Theory]
    [InlineData("MY", "f")]
    [InlineData("MZXW6YTBOI", "foobar")]
    [InlineData("MZXW6YTBOI======", "foobar")] // padding tollerato
    [InlineData("mzxw6ytboi", "foobar")]       // minuscole tollerate
    [InlineData("MZXW 6YTB OI", "foobar")]     // spazi tollerati
    public void Decode_e_tollerante_su_padding_maiuscole_e_spazi(string base32, string atteso)
        => Assert.Equal(atteso, Encoding.ASCII.GetString(Base32.Decode(base32)));

    [Fact]
    public void Encode_e_Decode_sono_inversi()
    {
        var originale = new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xFF, 0xAB, 0xCD };

        Assert.Equal(originale, Base32.Decode(Base32.Encode(originale)));
    }

    [Fact]
    public void Un_carattere_fuori_alfabeto_e_un_errore()
        => Assert.Throws<FormatException>(() => Base32.Decode("MZXW6YTB01"));
}

public class TotpTests
{
    /// <summary>
    /// Il segreto dei vettori di RFC 6238 appendice B: la stringa ASCII
    /// "12345678901234567890", che in Base32 è GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ.
    /// </summary>
    private static readonly string SegretoRfc =
        Base32.Encode(Encoding.ASCII.GetBytes("12345678901234567890"));

    [Fact]
    public void Il_segreto_dei_vettori_si_codifica_come_atteso()
        => Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", SegretoRfc);

    [Theory]
    // RFC 6238 appendice B, modalità SHA-1 a 8 cifre.
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1111111111L, "14050471")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(2000000000L, "69279037")]
    [InlineData(20000000000L, "65353130")]
    public void Codice_rispetta_i_vettori_ufficiali_di_RFC_6238(long secondiUnix, string atteso)
    {
        var istante = DateTimeOffset.FromUnixTimeSeconds(secondiUnix);

        Assert.Equal(atteso, Totp.Codice(SegretoRfc, istante, cifre: 8));
    }

    [Fact]
    public void Il_codice_a_sei_cifre_sono_le_ultime_sei_di_quello_a_otto()
    {
        var istante = DateTimeOffset.FromUnixTimeSeconds(59);

        Assert.Equal("287082", Totp.Codice(SegretoRfc, istante, cifre: 6));
    }

    [Fact]
    public void Il_codice_cambia_ogni_trenta_secondi_e_resta_stabile_dentro_la_finestra()
    {
        // Istante allineato al confine di finestra: 1_700_000_010 % 30 == 0.
        var t0 = DateTimeOffset.FromUnixTimeSeconds(1_700_000_010);

        var iniziale = Totp.Codice(SegretoRfc, t0);
        Assert.Equal(iniziale, Totp.Codice(SegretoRfc, t0.AddSeconds(29)));
        Assert.NotEqual(iniziale, Totp.Codice(SegretoRfc, t0.AddSeconds(30)));
    }

    // --- verifica ---

    [Fact]
    public void Verifica_accetta_il_codice_corrente()
    {
        var istante = DateTimeOffset.UtcNow;
        var codice = Totp.Codice(SegretoRfc, istante);

        Assert.True(Totp.Verifica(SegretoRfc, codice, istante));
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(30)]
    public void Verifica_tollera_una_finestra_per_lato(int scostamentoSecondi)
    {
        var adesso = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var codiceVicino = Totp.Codice(SegretoRfc, adesso.AddSeconds(scostamentoSecondi));

        Assert.True(Totp.Verifica(SegretoRfc, codiceVicino, adesso));
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Verifica_rifiuta_oltre_la_tolleranza(int scostamentoSecondi)
    {
        var adesso = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var codiceLontano = Totp.Codice(SegretoRfc, adesso.AddSeconds(scostamentoSecondi));

        Assert.False(Totp.Verifica(SegretoRfc, codiceLontano, adesso));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]      // troppo corto
    [InlineData("1234567")]    // troppo lungo
    [InlineData("abcdef")]     // non numerico
    [InlineData("12 34 56")]   // gli spazi si rimuovono, poi resta valido nel formato ma errato
    public void Verifica_rifiuta_i_codici_malformati(string? codice)
        => Assert.False(Totp.Verifica(SegretoRfc, codice, DateTimeOffset.UtcNow));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("segreto-non-base32!")]
    public void Verifica_rifiuta_i_segreti_assenti_o_malformati(string? segreto)
    {
        var codice = Totp.Codice(SegretoRfc, DateTimeOffset.UtcNow);

        Assert.False(Totp.Verifica(segreto, codice, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Un_segreto_diverso_non_valida_il_codice()
    {
        var istante = DateTimeOffset.UtcNow;
        var codice = Totp.Codice(SegretoRfc, istante);

        Assert.False(Totp.Verifica(Totp.GeneraSegreto(), codice, istante));
    }

    // --- generazione del segreto ---

    [Fact]
    public void GeneraSegreto_produce_segreti_diversi_e_decodificabili()
    {
        var a = Totp.GeneraSegreto();
        var b = Totp.GeneraSegreto();

        Assert.NotEqual(a, b);
        Assert.Equal(20, Base32.Decode(a).Length); // 160 bit
        Assert.Equal(32, a.Length);                // 20 byte in Base32 senza padding
    }

    // --- URI per il QR ---

    [Fact]
    public void UriOtpauth_contiene_i_parametri_che_le_app_si_aspettano()
    {
        var uri = Totp.UriOtpauth("ABCDEF", "Askii Platform", "mario@example.com");

        Assert.StartsWith("otpauth://totp/Askii%20Platform:mario%40example.com?", uri);
        Assert.Contains("secret=ABCDEF", uri);
        Assert.Contains("issuer=Askii%20Platform", uri);
        Assert.Contains("algorithm=SHA1", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }
}
