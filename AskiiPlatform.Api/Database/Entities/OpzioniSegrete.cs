namespace Askii.Database.Entities;

/// <summary>
/// Opzioni il cui valore è un segreto: vengono cifrate a riposo e mai
/// restituite in lettura. L'elenco è qui perché lo condividono la scrittura,
/// la lettura e l'invio email.
/// </summary>
public static class OpzioniSegrete
{
    private static readonly HashSet<string> Nomi = new(StringComparer.OrdinalIgnoreCase)
    {
        Option.Email.SMTP_PASS,
    };

    public static bool Contiene(string nome) => Nomi.Contains(nome);
}
