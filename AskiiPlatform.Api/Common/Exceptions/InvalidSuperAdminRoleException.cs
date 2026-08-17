namespace Askii.Common.Exceptions;

class InvalidSuperAdminRoleException: DomainException
{
    public InvalidSuperAdminRoleException(string user)
        : base($"L'utente {user} è il super amministratore, non più avere un ruolo diverso da {Roles.Admin}.")
    {
    }
}