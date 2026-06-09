using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PayrollCalc.API.Middleware;

/// <summary>
/// Глобальний обробник unhandled exceptions у HTTP pipeline.
/// Перехоплює виключення з будь-якого контролера, логує через ILogger,
/// повертає клієнту ProblemDetails (RFC 7807) з відповідним status code.
/// Зареєстрований у Program.cs через AddExceptionHandler + UseExceptionHandler.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Викликається ASP.NET для кожного unhandled exception. Маппить тип винятку
    /// у HTTP status, логує, формує ProblemDetails-відповідь у JSON.
    /// </summary>
    /// <param name="httpContext">HTTP-контекст поточного запиту.</param>
    /// <param name="exception">Виняток який треба обробити.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <returns>True — ми обробили, ASP.NET не шукає інших handler-ів.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Detail лише для "своїх" InvalidOperationException — їх повідомлення українські й
        // призначені користувачу (нема календаря, не налаштований параметр). Для решти Detail
        // не віддаємо: текст Npgsql/EF несе нутрощі схеми (імена constraint'ів, ключі).
        var (statusCode, title, detail) = exception switch
        {
            DbUpdateException => (StatusCodes.Status409Conflict, "Дані вже існують", (string?)null),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Некоректний запит або дані", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера", null),
        };
        _logger.LogError(
            exception,
            "Помилка обробки запиту {Method} {Path} → {StatusCode}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            statusCode
        );
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

}