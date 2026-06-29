namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Unit = gruppo di utenti. Ha membri e uno o più PM (manager) che ne gestiscono i membri
/// e assegnano i ticket. Un utente può appartenere a più Unit.
/// </summary>
public class Unit
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<UnitMembership> Memberships { get; set; } = new();
}

/// <summary>Appartenenza di un utente a una Unit. IsManager = il PM responsabile della Unit.</summary>
public class UnitMembership
{
    public int Id { get; set; }

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    /// <summary>True se l'utente è PM (manager) della Unit. Impostato dall'Admin.</summary>
    public bool IsManager { get; set; }
}
