namespace Askii.Common.Authorization;

/// <summary>
/// Permessi, cioè le singole azioni che si possono autorizzare.
///
/// Sostituiscono il controllo diretto sul ruolo negli endpoint. La differenza
/// che conta: "chiudere un ticket" o "reimpostare la password di un altro" sono
/// azioni, non ruoli, e con RequireRole si finisce a moltiplicare i ruoli oppure
/// a spargere controlli dentro gli handler. Qui il ruolo resta, ma diventa
/// soltanto un modo comodo di assegnare un insieme di permessi.
///
/// La convenzione del nome è `risorsa.azione`, con un livello ulteriore quando
/// serve distinguere (`users.tfa.reset`).
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string Read = "users.read";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";

        /// <summary>Rigenerare e reinviare il codice di attivazione.</summary>
        public const string Activate = "users.activate";

        /// <summary>Cambiare la password di un altro utente senza conoscerla.</summary>
        public const string ResetPassword = "users.password.reset";

        /// <summary>Azzerare il secondo fattore di un altro utente.</summary>
        public const string ResetTfa = "users.tfa.reset";
    }

    public static class Settings
    {
        public const string Read = "settings.read";
        public const string Update = "settings.update";
    }

    /// <summary>
    /// Tutti i permessi dichiarati. Serve al registro per rifiutare all'avvio i
    /// nomi che non esistono: un permesso scritto male in una mappa non darebbe
    /// errore, negherebbe l'accesso in silenzio.
    /// </summary>
    public static IReadOnlyCollection<string> Tutti { get; } =
    [
        Users.Read, Users.Create, Users.Update, Users.Delete,
        Users.Activate, Users.ResetPassword, Users.ResetTfa,
        Settings.Read, Settings.Update,
    ];
}
