using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Askii.Common.Extensions;

public static class EmailStringExtensions
{
    public static string NormalizeEmail(this string str)
    {
        return str.ToLowerInvariant().Trim();
    }

    /// <summary>
    /// MailAddress da solo non basta: accetta anche le forme con display name,
    /// quindi "Mario Rossi &lt;mario@example.com&gt;" passerebbe e finirebbe
    /// salvato tale e quale nella colonna Email. Qui si pretende che il testo
    /// sia esattamente l'indirizzo.
    /// </summary>
    public static bool IsValidEmail(this string? str)
    {
        if(string.IsNullOrWhiteSpace(str)) return false;
        if(!MailAddress.TryCreate(str, out var indirizzo) || indirizzo is null) return false;

        // Se MailAddress ha estratto un display name, o ha normalizzato in modo
        // diverso da quanto ricevuto, l'input non era un indirizzo puro.
        if(!string.IsNullOrEmpty(indirizzo.DisplayName)) return false;
        if(!string.Equals(indirizzo.Address, str, StringComparison.Ordinal)) return false;

        // Un dominio senza punto non è raggiungibile nella pratica.
        return indirizzo.Host.Contains('.');
    }
}