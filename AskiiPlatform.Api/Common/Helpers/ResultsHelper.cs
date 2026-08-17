namespace Askii.Common.Helpers;

public static class ResultsHelper
{
    public static IResult BadRequest(string msg) => Results.Problem(
        detail: msg,
        statusCode: StatusCodes.Status400BadRequest,
        title: "Bad request"
    );

    public static IResult Conflict(string msg) => Results.Problem(
        detail: msg,
        statusCode: StatusCodes.Status409Conflict,
        title: "Conflict"
    );

    public static IResult Unauthorized(string msg) => Results.Problem(
        detail: msg,
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Unauthorized"
    );

    public static IResult NotFound(string msg) => Results.Problem(
        detail: msg,
        statusCode: StatusCodes.Status404NotFound,
        title: "NotFound"
    );
}