using Microsoft.AspNetCore.DataProtection;

using Askii.Database.Entities;

namespace Askii.Common.Security;

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

/// <summary>
/// Cifra i valori di configurazione sensibili prima di scriverli a database, così
/// un dump della tabella Options non consegna la password SMTP in chiaro.
///
/// Usa Data Protection di ASP.NET Core, che è già nel framework: le chiavi
/// stanno fuori dal database, quindi chi legge solo le righe non può decifrare.
/// Attenzione all'altro lato della medaglia: perdere l'anello di chiavi rende i
/// valori irrecuperabili e vanno reinseriti.
/// </summary>
public interface ISecretProtector
{
    string Protect(string valoreInChiaro);

    /// <summary>
    /// Decifra un valore. Se non è cifrato lo restituisce così com'è, per non
    /// rompere le righe scritte prima dell'introduzione della cifratura.
    /// </summary>
    string Unprotect(string valoreSalvato);
}

public class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    /// <summary>
    /// Prefisso che marca i valori cifrati da noi: distingue un valore protetto
    /// da uno legacy in chiaro senza doverlo tentare di decifrare a vuoto.
    /// </summary>
    private const string Prefisso = "enc:v1:";

    private readonly IDataProtector _protector = provider.CreateProtector("Askii.Options.Secrets");

    public string Protect(string valoreInChiaro)
        => string.IsNullOrEmpty(valoreInChiaro)
            ? valoreInChiaro
            : Prefisso + _protector.Protect(valoreInChiaro);

    public string Unprotect(string valoreSalvato)
    {
        if (string.IsNullOrEmpty(valoreSalvato)) return valoreSalvato;
        if (!valoreSalvato.StartsWith(Prefisso, StringComparison.Ordinal)) return valoreSalvato;

        try
        {
            return _protector.Unprotect(valoreSalvato[Prefisso.Length..]);
        }
        catch (Exception)
        {
            // Anello di chiavi cambiato o perso: il valore non è più leggibile e
            // va reinserito. Restituire vuoto fa apparire l'opzione come non
            // configurata, che è la lettura corretta della situazione.
            return string.Empty;
        }
    }
}
