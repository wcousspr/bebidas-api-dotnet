using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace crudEbancoDist8.Middlewares;

internal static class RequestTrace
{
    public static string GetId(HttpContext context) =>
        Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity && activity.TraceId != default
            ? activity.TraceId.ToString()
            : context.TraceIdentifier;
}

internal sealed class ProblemTraceFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult { Value: ProblemDetails problem })
            problem.Extensions["traceId"] = RequestTrace.GetId(context.HttpContext);
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
