using Askii.Common.Security;
using Askii.Database.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Askii.ExternalServices;

public record EsitoInvio(bool Inviata, string? Errore = null)
{
    public static EsitoInvio Ok() => new(true);
    public static EsitoInvio Ko(string errore) => new(false, errore);
}

public interface IEmailSender
{
    Task<EsitoInvio> InviaAsync(string destinatario, string oggetto, string corpoTesto, CancellationToken ct = default);
    bool Configurato { get; }
}

/// <summary>
/// Invio SMTP con i parametri presi dalla tabella Options, quindi modificabili
/// dalla schermata Impostazioni senza riavviare l'applicazione.
///
/// Le impostazioni si leggono a ogni invio e non nel costruttore: essendo
/// modificabili a runtime, tenerle in cache significherebbe usare valori vecchi
/// fino al riavvio.
/// </summary>
public class SmtpEmailSender(
    Options options,
    IConfiguration configuration,
    ISecretProtector protector,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private string? Host => Vuoto(options.GetValue<string>(Option.Email.SMTP_HOST));
    private string? Utente => Vuoto(options.GetValue<string>(Option.Email.SMTP_USER));
    private string? Password =>
        Vuoto(protector.Unprotect(options.GetValue<string>(Option.Email.SMTP_PASS) ?? string.Empty));

    private int Porta
    {
        get
        {
            var grezza = options.GetValue<string>(Option.Email.SMTP_PORT);
            return int.TryParse(grezza, out var p) && p > 0 ? p : 587;
        }
    }

    public bool Configurato => Host is not null;

    private static string? Vuoto(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;

    public async Task<EsitoInvio> InviaAsync(
        string destinatario, string oggetto, string corpoTesto, CancellationToken ct = default)
    {
        if (!Configurato)
        {
            // In sviluppo il codice viene messo nel log, altrimenti sarebbe
            // impossibile provare il flusso senza un server SMTP. In produzione
            // non si logga nulla: sarebbe un segreto scritto su file.
            if (configuration["ASPNETCORE_ENVIRONMENT"] == "Development"
                || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                logger.LogWarning(
                    "SMTP non configurato. Email NON inviata a {Destinatario}. Contenuto (solo in sviluppo): {Oggetto} | {Corpo}",
                    destinatario, oggetto, corpoTesto);
            }

            return EsitoInvio.Ko("Server di posta non configurato: impostalo dalla schermata Impostazioni.");
        }

        var messaggio = new MimeMessage();
        messaggio.From.Add(MailboxAddress.Parse(Utente ?? $"no-reply@{Host}"));
        messaggio.To.Add(MailboxAddress.Parse(destinatario));
        messaggio.Subject = oggetto;
        messaggio.Body = new TextPart("plain") { Text = corpoTesto };

        try
        {
            using var client = new SmtpClient();

            // 465 è TLS implicito, gli altri porti negoziano STARTTLS quando il
            // server lo offre.
            var sicurezza = Porta == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(Host, Porta, sicurezza, ct);

            if (Utente is not null && Password is not null)
            {
                await client.AuthenticateAsync(Utente, Password, ct);
            }

            await client.SendAsync(messaggio, ct);
            await client.DisconnectAsync(true, ct);

            return EsitoInvio.Ok();
        }
        catch (Exception ex)
        {
            // Il messaggio del server SMTP può contenere dettagli infrastrutturali:
            // resta nel log, al chiamante va una descrizione generica.
            logger.LogError(ex, "Invio email a {Destinatario} fallito", destinatario);
            return EsitoInvio.Ko("Invio della email non riuscito. Controlla i parametri SMTP.");
        }
    }
}
