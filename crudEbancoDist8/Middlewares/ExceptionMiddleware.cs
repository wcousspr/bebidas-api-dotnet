using System.Diagnostics;
using System.Text.Json;
using crudEbancoDist8.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;

namespace crudEbancoDist8.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions ProblemJson = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        var traceId = RequestTrace.GetId(context);
        Exception? failure = null;
        var cancelled = false;

        try
        {
            await next(context);
        }
        catch (Exception ex) when (context.RequestAborted.IsCancellationRequested &&
            ex is OperationCanceledException or IOException)
        {
            cancelled = true;
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            failure = ex;
            if (context.Response.HasStarted)
            {
                context.Abort();
            }
            else
            {
                await WriteProblemAsync(context, traceId);
            }
        }
        finally
        {
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(sem rota)";
            var status = cancelled ? StatusCodes.Status499ClientClosedRequest : context.Response.StatusCode;
            var level = failure is not null || (status >= 500 && status != 503) ? LogLevel.Error
                : status is 429 or 503 ? LogLevel.Warning : LogLevel.Information;
            var successfulProbe = failure is null && status is >= 200 and < 300 &&
                route is "/health/live" or "/health/ready";
            if (!successfulProbe)
            {
                // Provider exceptions can embed connection details, SQL or user input.
                logger.Log(level, "HTTP {Method} {Route} retornou {StatusCode} em {ElapsedMs} ms. TraceId: {TraceId}. ErrorType: {ErrorType}. SqlErrorNumber: {SqlErrorNumber}. ReadinessCause: {ReadinessCause}",
                    SafeMethod(context.Request.Method), route, status, Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    traceId, failure?.GetType().Name, GetSqlErrorNumber(failure),
                    context.Items[OperationalHealthExtensions.ReadinessCauseKey]);
            }
        }
    }

    private static string SafeMethod(string method) => method switch
    {
        "GET" or "HEAD" or "POST" or "PUT" or "PATCH" or "DELETE" or "OPTIONS" or "TRACE" or "CONNECT" => method,
        _ => "OTHER"
    };

    private static int? GetSqlErrorNumber(Exception? error)
    {
        for (var cause = error; cause is not null; cause = cause.InnerException)
        {
            if (cause is SqlException sql)
                return sql.Number;
        }
        return null;
    }

    private static async Task WriteProblemAsync(HttpContext context, string traceId)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocorreu um erro inesperado.",
            Detail = "Por favor, tente novamente mais tarde ou entre em contato com o suporte.",
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = traceId;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        try
        {
            await JsonSerializer.SerializeAsync(context.Response.Body, problem, ProblemJson, context.RequestAborted);
        }
        catch (Exception ex) when (context.RequestAborted.IsCancellationRequested &&
            ex is OperationCanceledException or IOException)
        {
            // The client disconnected while the error response was being written.
        }
    }
}
