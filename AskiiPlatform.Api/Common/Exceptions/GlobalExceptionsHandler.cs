using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace Askii.Common.Exceptions;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        logger.LogError(exception, "Si è verificato un errore: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            // Se è un errore di risorsa non trovata -> 404 Not Found
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Risorsa non trovata",
                Detail = notFound.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"
            },

            // Se è una violazione di regole del Model / Dominio -> 400 Bad Requestp
            DomainException domainEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Violazione regola di business",
                Detail = domainEx.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
            },

            // Richiesta malformata (es. query string che non si converte nel tipo
            // atteso) -> 400. Senza questo ramo finirebbe nel 500 generico, e un
            // errore del client verrebbe segnalato come guasto del server.
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Richiesta non valida",
                Detail = badRequest.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
            },

            // Qualsiasi altro errore non previsto (es. DB giù, NullReference) -> 500 Internal Server Error
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Errore interno del server",
                Detail = "Si è verificato un errore imprevisto.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}