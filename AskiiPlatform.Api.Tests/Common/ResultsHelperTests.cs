using Askii.Common.Helpers;

namespace Askii.Tests.Common;

public class ResultsHelperTests
{
    [Fact]
    public void BadRequest_produce_problem_400()
    {
        var problem = Assert.IsType<ProblemHttpResult>(ResultsHelper.BadRequest("messaggio"));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Bad request", problem.ProblemDetails.Title);
        Assert.Equal("messaggio", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Conflict_produce_problem_409()
    {
        var problem = Assert.IsType<ProblemHttpResult>(ResultsHelper.Conflict("duplicato"));

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("Conflict", problem.ProblemDetails.Title);
        Assert.Equal("duplicato", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Unauthorized_produce_problem_401()
    {
        var problem = Assert.IsType<ProblemHttpResult>(ResultsHelper.Unauthorized("non autorizzato"));

        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
        Assert.Equal("Unauthorized", problem.ProblemDetails.Title);
    }

    [Fact]
    public void NotFound_produce_problem_404()
    {
        var problem = Assert.IsType<ProblemHttpResult>(ResultsHelper.NotFound("assente"));

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("NotFound", problem.ProblemDetails.Title);
    }
}
