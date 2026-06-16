using Aski.Shared;

namespace Aski.ControlPlane.Entities;

/// <summary>
/// Utente del Control Plane (login al pannello).
///
/// Due tipi, distinti da <see cref="Role"/>:
///  - SuperAdmin: proprietario della piattaforma, nessun <see cref="TenantId"/>.
///  - TenantOwner: cliente self-service, legato al proprio <see cref="Tenant"/>.
///
/// La registrazione self-service crea contestualmente un Tenant e il suo TenantOwner.
/// Le password sono hashate con BCrypt.
/// </summary>
public class PortalUser
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public string? DisplayName { get; set; }

    public PortalUserRole Role { get; set; }

    /// <summary>Org del cliente (null per i SuperAdmin).</summary>
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
