using System.Security.Cryptography;

namespace Askii.Common.Security;

/// <summary>
/// TOTP secondo RFC 6238, compatibile con Google Authenticator, Authy, Aegis e
/// simili: HMAC-SHA1, 6 cifre, finestra di 30 secondi, che sono i valori che
/// quelle app assumono quando l'URI otpauth non li dichiara.
///
/// Implementato invece di prendere una dipendenza perché l'algoritmo è breve e
/// completamente specificato: i test lo verificano contro i vettori ufficiali
/// dell'appendice B dell'RFC, che è una garanzia più forte di quella che darebbe
/// una libreria non verificata.
/// </summary>
public static class Totp
{
    public const int CifrePredefinite = 6;
    public const int PeriodoSecondi = 30;

    /// <summary>
    /// 20 byte = 160 bit, la dimensione raccomandata da RFC 4226 §4 per HMAC-SHA1.
    /// </summary>
    public static string GeneraSegreto(int byteCasuali = 20)
        => Base32.Encode(RandomNumberGenerator.GetBytes(byteCasuali));

    public static string Codice(
        string segretoBase32,
        DateTimeOffset? istante = null,
        int cifre = CifrePredefinite)
    {
        var contatore = Contatore(istante ?? DateTimeOffset.UtcNow);
        return CodiceDaContatore(Base32.Decode(segretoBase32), contatore, cifre);
    }

    /// <summary>
    /// Confronta il codice con quello atteso, accettando anche le finestre
    /// adiacenti: senza tolleranza un orologio sfasato di pochi secondi farebbe
    /// fallire codici legittimi. Una finestra per lato copre ±30 secondi, che è
    /// il compromesso raccomandato dall'RFC §5.2.
    /// </summary>
    public static bool Verifica(
        string? segretoBase32,
        string? codice,
        DateTimeOffset? istante = null,
        int finestreTolleranza = 1,
        int cifre = CifrePredefinite)
    {
        if (string.IsNullOrWhiteSpace(segretoBase32) || string.IsNullOrWhiteSpace(codice))
        {
            return false;
        }

        var normalizzato = codice.Replace(" ", "").Trim();
        if (normalizzato.Length != cifre || !normalizzato.All(char.IsAsciiDigit)) return false;

        byte[] segreto;
        try { segreto = Base32.Decode(segretoBase32); }
        catch (FormatException) { return false; }
        if (segreto.Length == 0) return false;

        var contatore = Contatore(istante ?? DateTimeOffset.UtcNow);

        for (var scostamento = -finestreTolleranza; scostamento <= finestreTolleranza; scostamento++)
        {
            var atteso = CodiceDaContatore(segreto, contatore + scostamento, cifre);

            // Confronto a tempo costante: un confronto stringa normale terminerebbe
            // al primo carattere diverso, rivelando quante cifre sono corrette.
            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(atteso),
                    System.Text.Encoding.ASCII.GetBytes(normalizzato)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// URI otpauth:// che le app leggono dal QR. L'emittente compare due volte
    /// per convenzione: come prefisso dell'etichetta e come parametro, perché le
    /// app più vecchie leggono solo il primo.
    /// </summary>
    public static string UriOtpauth(string segretoBase32, string emittente, string account)
    {
        var e = Uri.EscapeDataString(emittente);
        var a = Uri.EscapeDataString(account);

        return $"otpauth://totp/{e}:{a}"
             + $"?secret={segretoBase32}"
             + $"&issuer={e}"
             + $"&algorithm=SHA1"
             + $"&digits={CifrePredefinite}"
             + $"&period={PeriodoSecondi}";
    }

    private static long Contatore(DateTimeOffset istante)
        => istante.ToUnixTimeSeconds() / PeriodoSecondi;

    private static string CodiceDaContatore(byte[] segreto, long contatore, int cifre)
    {
        // Il contatore viaggia come intero a 64 bit big-endian (RFC 4226 §5.1).
        var messaggio = BitConverter.GetBytes(contatore);
        if (BitConverter.IsLittleEndian) Array.Reverse(messaggio);

        var hash = HMACSHA1.HashData(segreto, messaggio);

        // Troncamento dinamico: i 4 bit finali indicano da dove leggere.
        var offset = hash[^1] & 0x0F;
        var binario = ((hash[offset] & 0x7F) << 24)
                    | ((hash[offset + 1] & 0xFF) << 16)
                    | ((hash[offset + 2] & 0xFF) << 8)
                    | (hash[offset + 3] & 0xFF);

        var modulo = (int)Math.Pow(10, cifre);
        return (binario % modulo).ToString().PadLeft(cifre, '0');
    }
}
