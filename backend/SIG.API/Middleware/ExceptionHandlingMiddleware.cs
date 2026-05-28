using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIG.Domain.Exceptions;

namespace SIG.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException dex)
        {
            ctx.Response.StatusCode = dex.HttpStatusCode;
            var pd = new ProblemDetails { Status = dex.HttpStatusCode, Title = dex.Message };
            pd.Extensions["code"] = dex.Code;
            await ctx.Response.WriteAsJsonAsync(pd);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected");
            ctx.Response.StatusCode = 412;
            var pd = new ProblemDetails { Status = 412, Title = "Concurrency conflict" };
            pd.Extensions["code"] = "concurrency_conflict";
            await ctx.Response.WriteAsJsonAsync(pd);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error");
            ctx.Response.StatusCode = 500;
            var pd = new ProblemDetails { Status = 500, Title = "Database error" };
            pd.Extensions["code"] = "database_error";
            await ctx.Response.WriteAsJsonAsync(pd);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            ctx.Response.StatusCode = 400;
            var pd = new ProblemDetails { Status = 400, Title = ex.Message };
            pd.Extensions["code"] = "invalid_operation";
            await ctx.Response.WriteAsJsonAsync(pd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in pipeline");
            ctx.Response.StatusCode = 500;
            var pd = new ProblemDetails { Status = 500, Title = "Internal server error" };
            pd.Extensions["code"] = "internal_error";
            await ctx.Response.WriteAsJsonAsync(pd);
        }
    }
}
