namespace Askii.Common.Exceptions;

// Classe base astratta per identificare che è un errore di business e non un crash di sistema
public abstract class DomainException(string message) : Exception(message);