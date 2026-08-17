namespace Askii.Common.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, object key) : base ($"La risorsa '{resourceName}' con identificativo '{key}' non è stata trovata.") {}
    public NotFoundException() : base ("Risolrsa non trovata") {}

}