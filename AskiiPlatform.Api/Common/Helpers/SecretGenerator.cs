using System.Security.Cryptography;

namespace Askii.Common.Helpers;

public static class SecretGenerator
{
    // Esclusi i caratteri ambigui (I, l, 1, O, 0): questi valori finiscono
    // nelle mail e vengono ricopiati a mano.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    /// <summary>Codice di attivazione: 22 caratteri = 128 bit di entropia.</summary>
    public static string ActivationCode() => RandomNumberGenerator.GetString(Alphabet, 22);

    /// <summary>Password temporanea: 16 caratteri = 93 bit.</summary>
    public static string TemporaryPassword() => RandomNumberGenerator.GetString(Alphabet, 16);

    /// <summary>
    /// Codice numerico per l'OTP via email. Solo cifre perché va digitato in
    /// fretta, spesso da telefono: 6 cifre sono ~20 bit, quindi la difesa non è
    /// l'entropia ma la scadenza breve e il limite sui tentativi.
    /// </summary>
    public static string NumericCode(int cifre = 6) => RandomNumberGenerator.GetString("0123456789", cifre);
}