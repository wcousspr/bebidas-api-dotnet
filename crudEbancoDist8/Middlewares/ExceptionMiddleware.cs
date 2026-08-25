using System.Text.Json;
using Microsoft.AspNetCore.Mvc;



namespace crudEbancoDist8.Middlewares;



public class ExceptionMiddleware
{

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro não tratado em {Method} {Path}",
                context.Request.Method,
                context.Request.Path

                );

            await HandleExceptionAsync(context);

        }

    }

    private static async Task HandleExceptionAsync(HttpContext context)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocorreu um erro inesperado.",
            Detail = "Por favor, tente novamente mais tarde ou entre em contato com o suporte.",
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
