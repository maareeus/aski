namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Ruoli applicativi.
/// Admin  = gestione totale (utenti, aziende, software, unit, rubrica, ticket).
/// PM     = Project Manager: gestisce i membri delle Unit che gli sono affidate e
///          assegna i ticket ai propri utenti (utente+unit).
/// Agent  = operatore: vede/lavora i ticket dei software assegnati e quelli assegnati a lui.
/// Client = cliente: apre e segue solo i propri ticket; vede la rubrica della sua azienda.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string PM = "PM";
    public const string Agent = "Agent";
    public const string Client = "Client";

    public static readonly string[] All = { Admin, PM, Agent, Client };

    /// <summary>Ruoli "staff" (operatori interni).</summary>
    public static readonly string[] Staff = { Admin, PM, Agent };
}
