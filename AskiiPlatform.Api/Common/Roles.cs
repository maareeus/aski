namespace Askii.Common;

public static class Roles
{
    /// <summary>
    /// Ruolo riservato agli amministratori con gestione degli utenti ecc
    /// </summary>
    public const string Admin = "Admin";
    /// <summary>
    /// Accedono al portale operatori e hanno la gestione dei ticket
    /// </summary>
    public const string Operator = "Operator";
    /// <summary>
    /// Hanno accesso solo alla gestione del customer portal
    /// </summary>
    public const string Client = "Client";

    public static readonly IReadOnlyCollection<string> All = [Admin, Operator, Client];
}
