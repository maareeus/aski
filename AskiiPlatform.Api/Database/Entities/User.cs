using Askii.Common;
using Askii.Common.Exceptions;
using Askii.Common.Extensions;
using Askii.Database.Entities.Common;
using Askii.Common.Helpers;
using Askii.Common.Security;
using Askii.Features.Auth;

namespace Askii.Database.Entities;

public class User : BaseEntity
{
    public string Email {get;private set;} = string.Empty;
    public string PasswordHash {get;private set;} = string.Empty;
    public string Name {get;  set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
    public string Role {get;private set;} = Roles.Client;
    public bool IsSuperAdmin {get; private set;} = false;
    public bool IsActive {get;set;} = false;
    public DateTime? LastLoginUtc {get; private set;}

    /// <summary>
    /// Lista di metodi di 2FA disponibili, se almeno uno è presente la login ok
    /// risponde con la lista di 2FA e non con il token
    /// </summary>
    public List<TFA_Available> TFA_Availables {get;set;}= new();

    /// <summary>
    /// Cambia quando cambia qualcosa che rende non più validi i token già
    /// emessi: password e ruolo. Viene messo nel JWT e confrontato a ogni
    /// richiesta, così un token vecchio smette di funzionare senza aspettarne
    /// la scadenza naturale.
    /// </summary>
    public string SecurityStamp {get; private set;} = Guid.NewGuid().ToString("N");

    /// <summary>Hash del codice di attivazione. Non si salva in chiaro.</summary>
    public string? ActivationCodeHash {get; private set;}

    public DateTime? ActivationCodeExpiresUtc {get; private set;}

    // --- supporto ai metodi di 2FA ---

    /// <summary>
    /// Segreto TOTP in Base32. Viene creato quando l'utente avvia
    /// l'associazione dell'app e resta non confermato finché non digita un
    /// codice valido: solo allora AUTHENTICATOR_APP entra fra i metodi attivi.
    /// </summary>
    public string? TotpSecret {get; private set;}

    /// <summary>Hash del codice OTP inviato per email. Non si salva in chiaro.</summary>
    public string? EmailOtpHash {get; private set;}

    public DateTime? EmailOtpExpiresUtc {get; private set;}

    /// <summary>
    /// Tentativi falliti sul codice corrente: serve a impedire che un codice a
    /// sei cifre venga indovinato per tentativi.
    /// </summary>
    public int EmailOtpAttempts {get; private set;}

    public string FullName { get => $"{Name} {LastName}";}

    public bool TfaEnabled => TFA_Availables.Count > 0;

    /// <summary>Il segreto esiste ma l'utente non ha ancora confermato con un codice.</summary>
    public bool HasPendingTotp => TotpSecret is not null
        && !TFA_Availables.Contains(TFA_Available.AUTHENTICATOR_APP);

    private User() {}

    public static User Create(
        string email,
        string password,
        string? name,
        string? lastName,
        string? role
    )
    {
        if(!Roles.All.Any(x => x == role))
        {
            throw new InvalidUserRoleException(role ?? string.Empty, Roles.All);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = string.Empty,
            Name = name ?? string.Empty,
            LastName = lastName ?? string.Empty,
            Role = role ?? Roles.Client,
            IsSuperAdmin = false,
            IsActive = false
        };
        user.SetPassword(password);
        return user;
    }

    public static User CreateSuperAdmin(
        string email,
        string password,
        string? name,
        string? lastName
    )
    {
        User superAdmin = User.Create(email, password, name, lastName, Roles.Admin);
        superAdmin.IsActive = true;
        superAdmin.IsSuperAdmin = true;

        return superAdmin;
    }

    public void SetPassword(string psw)
    {
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(psw);
        RevokeSessions();
    }

    /// <summary>
    /// Rende inutilizzabili i token già emessi per questo utente. Chiamato dai
    /// cambi che alterano credenziali o autorizzazioni.
    /// </summary>
    public void RevokeSessions()
    {
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public void SetEmail(string email)
    {
        if(!email.NormalizeEmail().IsValidEmail()) throw new InvalidEmailException(email.NormalizeEmail());
        Email = email.NormalizeEmail();
    }

    public void UpdateRole(string role)
    {
        if(!Roles.All.Any(x => x == role))
        {
            throw new InvalidUserRoleException(role, Roles.All);
        }

        if(IsSuperAdmin && role != Roles.Admin)
        {
            throw new InvalidSuperAdminRoleException(Email);
        }

        Role = role;
        // Il ruolo è dentro il token: senza revoca, un declassamento resterebbe
        // senza effetto fino alla scadenza.
        RevokeSessions();
    }

    public bool VerifyPassword(string plainPassword) => BCrypt.Net.BCrypt.Verify(plainPassword, PasswordHash);

    // --- attivazione con codice ---

    /// <summary>
    /// Emette un codice di attivazione e ne conserva solo l'hash. Restituisce il
    /// valore in chiaro una volta sola, per l'invio.
    /// </summary>
    public string IssueActivationCode(int validitaGiorni = 7, DateTime? adesso = null)
    {
        var codice = SecretGenerator.ActivationCode();

        ActivationCodeHash = BCrypt.Net.BCrypt.HashPassword(codice);
        ActivationCodeExpiresUtc = (adesso ?? DateTime.UtcNow).AddDays(validitaGiorni);

        return codice;
    }

    /// <summary>
    /// Attiva l'account verificando il codice e impostando la password scelta
    /// dall'utente. Il codice è monouso.
    ///
    /// È l'utente a scegliere la password, non l'amministratore: la temporanea
    /// generata alla creazione non è nota a nessuno e serve solo a impedire
    /// l'accesso prima dell'attivazione.
    /// </summary>
    public bool TryActivate(string? codice, string password, DateTime? adesso = null)
    {
        if (ActivationCodeHash is null || ActivationCodeExpiresUtc is null) return false;
        if (string.IsNullOrWhiteSpace(codice)) return false;
        if ((adesso ?? DateTime.UtcNow) > ActivationCodeExpiresUtc.Value) return false;
        if (!BCrypt.Net.BCrypt.Verify(codice.Trim(), ActivationCodeHash)) return false;

        SetPassword(password);
        IsActive = true;
        ClearActivationCode();
        return true;
    }

    public void ClearActivationCode()
    {
        ActivationCodeHash = null;
        ActivationCodeExpiresUtc = null;
    }

    public bool HasPendingActivation => ActivationCodeHash is not null;

    // --- 2FA: app di authenticator ---

    /// <summary>
    /// Genera un nuovo segreto e lo lascia in attesa di conferma. Rigenerarlo
    /// invalida quello precedente: chi ha già inquadrato il vecchio QR deve
    /// rifarlo, ed è il comportamento voluto se si ricomincia l'associazione.
    /// </summary>
    public string StartTotpEnrollment()
    {
        TotpSecret = Totp.GeneraSegreto();
        TFA_Availables.Remove(TFA_Available.AUTHENTICATOR_APP);
        return TotpSecret;
    }

    /// <summary>
    /// Conferma l'associazione verificando un codice generato dall'app. Senza
    /// questo passaggio il metodo non viene attivato, altrimenti un errore di
    /// configurazione lascerebbe l'utente chiuso fuori dal proprio account.
    /// </summary>
    public bool ConfirmTotp(string? codice, DateTimeOffset? istante = null)
    {
        if (TotpSecret is null) return false;
        if (!Totp.Verifica(TotpSecret, codice, istante)) return false;

        if (!TFA_Availables.Contains(TFA_Available.AUTHENTICATOR_APP))
        {
            TFA_Availables.Add(TFA_Available.AUTHENTICATOR_APP);
        }
        return true;
    }

    public bool VerifyTotp(string? codice, DateTimeOffset? istante = null)
        => TFA_Availables.Contains(TFA_Available.AUTHENTICATOR_APP)
           && Totp.Verifica(TotpSecret, codice, istante);

    public void DisableTotp()
    {
        TotpSecret = null;
        TFA_Availables.Remove(TFA_Available.AUTHENTICATOR_APP);
    }

    // --- 2FA: codice via email ---

    public void EnableEmailOtp()
    {
        if (!TFA_Availables.Contains(TFA_Available.EMAIL_OTP))
        {
            TFA_Availables.Add(TFA_Available.EMAIL_OTP);
        }
    }

    public void DisableEmailOtp()
    {
        TFA_Availables.Remove(TFA_Available.EMAIL_OTP);
        ClearEmailOtp();
    }

    /// <summary>
    /// Emette un codice e ne conserva solo l'hash, con scadenza. Restituisce il
    /// codice in chiaro una volta sola, per l'invio.
    /// </summary>
    public string IssueEmailOtp(int validitaMinuti = 5, DateTime? adesso = null)
    {
        var codice = SecretGenerator.NumericCode(6);
        var ora = adesso ?? DateTime.UtcNow;

        EmailOtpHash = BCrypt.Net.BCrypt.HashPassword(codice);
        EmailOtpExpiresUtc = ora.AddMinutes(validitaMinuti);
        EmailOtpAttempts = 0;

        return codice;
    }

    public const int MaxEmailOtpAttempts = 5;

    /// <summary>
    /// Verifica il codice e lo consuma: un codice valido non è riutilizzabile, e
    /// dopo troppi tentativi falliti viene invalidato comunque.
    /// </summary>
    public bool VerifyEmailOtp(string? codice, DateTime? adesso = null)
    {
        if (!TFA_Availables.Contains(TFA_Available.EMAIL_OTP)) return false;
        if (EmailOtpHash is null || EmailOtpExpiresUtc is null) return false;
        if (string.IsNullOrWhiteSpace(codice)) return false;

        var ora = adesso ?? DateTime.UtcNow;
        if (ora > EmailOtpExpiresUtc.Value) { ClearEmailOtp(); return false; }

        if (EmailOtpAttempts >= MaxEmailOtpAttempts) { ClearEmailOtp(); return false; }

        if (!BCrypt.Net.BCrypt.Verify(codice.Trim(), EmailOtpHash))
        {
            EmailOtpAttempts++;
            if (EmailOtpAttempts >= MaxEmailOtpAttempts) ClearEmailOtp();
            return false;
        }

        ClearEmailOtp();
        return true;
    }

    public void ClearEmailOtp()
    {
        EmailOtpHash = null;
        EmailOtpExpiresUtc = null;
        EmailOtpAttempts = 0;
    }

    /// <summary>
    /// Disattiva ogni metodo e cancella i segreti. È il percorso di recupero per
    /// un utente che ha perso l'accesso al secondo fattore.
    /// </summary>
    public void DisableAllTfa()
    {
        TFA_Availables.Clear();
        TotpSecret = null;
        ClearEmailOtp();
    }

    public void RecordLogin()
    {
        LastLoginUtc = DateTime.UtcNow;
    }
}