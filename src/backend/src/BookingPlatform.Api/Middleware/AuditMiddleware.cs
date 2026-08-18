using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BookingPlatform.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly IAuditLogger _auditLogger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger, IAuditLogger auditLogger)
    {
        _next = next;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip non-mutating requests
        if (!IsMutatingMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Skip health checks
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                           ?? Activity.Current?.TraceId.ToString()
                           ?? Guid.NewGuid().ToString();

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        // Capture request body
        string requestBody = await GetRequestBodyAsync(context.Request);

        // Capture response body
        var originalBodyStream = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var startTime = DateTimeOffset.UtcNow;
        Exception? exception = null;
        int statusCode = 200;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            exception = ex;
            statusCode = 500;
            throw;
        }
        finally
        {
            var elapsed = DateTimeOffset.UtcNow - startTime;

            // Read response body
            responseBodyStream.Position = 0;
            string responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalBodyStream);

            // Don't log if response is too large (> 10KB)
            if (responseBody.Length > 10240)
            {
                responseBody = "[Response body too large]";
            }

            // Extract user info
            var userId = GetUserId(context.User);
            var ipAddress = GetClientIp(context);
            var userAgent = context.Request.Headers.UserAgent.ToString();

            // Determine entity from path
            var (entityName, entityId) = ExtractEntityInfo(context.Request.Path);

            var auditEntry = new AuditLogEntry
            {
                CorrelationId = Guid.Parse(correlationId),
                ActorUserId = userId,
                Action = $"{context.Request.Method} {context.Request.Path}",
                EntityName = entityName,
                EntityId = entityId,
                BeforeValue = null, // Middleware doesn't know before state
                AfterValue = TryParseJson(responseBody),
                Reason = null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                StatusCode = statusCode,
                Exception = exception?.ToString(),
                DurationMs = (long)elapsed.TotalMilliseconds,
                CreatedAt = startTime
            };

            try
            {
                await _auditLogger.LogAsync(auditEntry);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "Failed to write audit log");
            }

            // Log to Serilog as well
            _logger.LogInformation(
                "HTTP {Method} {Path} => {StatusCode} in {Duration}ms (CorrelationId: {CorrelationId})",
                context.Request.Method, context.Request.Path, statusCode, elapsed.TotalMilliseconds, correlationId);
        }
    }

    private static bool IsMutatingMethod(string method)
    {
        return method is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    private static async Task<string> GetRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == 0 || request.ContentLength > 10240)
        {
            return "[Request body empty or too large]";
        }

        request.EnableBuffering();
        var buffer = new byte[request.ContentLength.GetValueOrDefault()];
        await request.Body.ReadAsync(buffer);
        request.Body.Position = 0;
        return Encoding.UTF8.GetString(buffer);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("sub") ?? user.FindFirst("oid") ?? user.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var id) ? id : null;
    }

    private static string? GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static (string entityName, Guid? entityId) ExtractEntityInfo(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        // /api/v1/reservations/{id} -> entity: reservations, id: {id}
        if (segments.Length >= 3 && segments[0] == "api" && segments[1].StartsWith("v"))
        {
            var entity = segments[2].ToLowerInvariant();
            if (segments.Length >= 4 && Guid.TryParse(segments[3], out var id))
            {
                return (entity, id);
            }
            return (entity, null);
        }

        return ("unknown", null);
    }

    private static object? TryParseJson(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return json;
        }
    }
}

public record AuditLogEntry
{
    public Guid CorrelationId { get; init; }
    public Guid? ActorUserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public object? BeforeValue { get; init; }
    public object? AfterValue { get; init; }
    public string? Reason { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public int StatusCode { get; init; }
    public string? Exception { get; init; }
    public long DurationMs { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry);
    Task LogDomainEventAsync(IDomainEvent domainEvent, IAggregateRoot entity, CancellationToken ct);
}