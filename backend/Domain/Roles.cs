namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Ruoli applicativi del sistema di assistenza.
/// Admin  = gestione totale (utenti, software, clienti, ticket).
/// Agent  = operatore: lavora i ticket assegnati/di competenza.
/// Client = cliente: apre ticket per la propria azienda e li segue.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Agent = "Agent";
    public const string Client = "Client";

    public static readonly string[] All = { Admin, Agent, Client };
}
