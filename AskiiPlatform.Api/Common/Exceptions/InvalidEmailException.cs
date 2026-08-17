namespace Askii.Common.Exceptions;

class InvalidEmailException: DomainException
{
    public InvalidEmailException(string invalidEmail)
        : base($"La mail inserita '{invalidEmail}' non è valida.")
    {
    }
}