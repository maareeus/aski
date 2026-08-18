using FluentValidation;

namespace Askii.Common.Validation;

/// <summary>
/// Esegue il validatore registrato per il tipo di richiesta prima di entrare
/// nell'handler.
///
/// Sostituisce i controlli sparsi dentro gli endpoint e, soprattutto, chiude il
/// caso in cui un campo assente nel JSON arrivava null nonostante il tipo non
/// nullable e faceva esplodere l'handler con un 500 invece di un 400.
/// </summary>
public class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var richiesta = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (richiesta is null)
        {
            return TypedResults.Problem(
                detail: $"Corpo della richiesta assente o non deserializzabile in {typeof(TRequest).Name}.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Richiesta non valida");
        }

        var esito = await validator.ValidateAsync(richiesta, context.HttpContext.RequestAborted);
        if (esito.IsValid) return await next(context);

        // ValidationProblem produce il formato RFC 7807 con il dizionario
        // `errors`, che il client può usare per evidenziare i singoli campi.
        var errori = esito.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return TypedResults.ValidationProblem(errori, title: "Dati non validi");
    }
}

public static class ValidationFilterExtensions
{
    /// <summary>
    /// Collega la validazione a un endpoint. Richiede un IValidator&lt;TRequest&gt;
    /// registrato, altrimenti la risoluzione del filtro falla all'avvio: è
    /// voluto, meglio un errore in partenza che una validazione silenziosamente
    /// assente.
    /// </summary>
    public static TBuilder Validating<TBuilder, TRequest>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter<TBuilder, ValidationFilter<TRequest>>();
        return builder;
    }
}
