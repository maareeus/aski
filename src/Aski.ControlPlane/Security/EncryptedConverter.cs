using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aski.ControlPlane.Security;

/// <summary>
/// ValueConverter EF Core che cifra/decifra una stringa a riposo tramite
/// ASP.NET DataProtection. In memoria la property è plaintext; nella colonna
/// del DB è ciphertext. Applicato alle chiavi segrete di Stripe.
///
/// Nota: i valori cifrati non sono interrogabili (no WHERE su questi campi),
/// limite accettabile per chiavi/segreti che non si filtrano mai.
/// </summary>
public sealed class EncryptedConverter : ValueConverter<string, string>
{
    public EncryptedConverter(IDataProtector protector)
        : base(
            plaintext => protector.Protect(plaintext),
            ciphertext => protector.Unprotect(ciphertext))
    {
    }

    /// <summary>Purpose stabile per il protector: non cambiarlo o i dati esistenti non si decifrano.</summary>
    public const string Purpose = "Aski.StripeSecrets.v1";
}
