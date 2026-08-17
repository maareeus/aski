namespace Askii.Common.Exceptions;

class InvalidUserRoleException: DomainException
{
    public InvalidUserRoleException(string invalidRole, IEnumerable<string> allowedRoles)
        : base($"Il ruolo '{invalidRole}' non è valido. I ruoli consentiti sono: {string.Join(", ", allowedRoles)}.")
    {
    }
}