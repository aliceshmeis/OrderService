using Serilog;
using System.Text;
using System.Text.Json;

namespace InventoryService.API.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly HashSet<string> _sensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Cookie", "X-API-Key", "X-Auth-Token"
        };

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for certain paths
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";

            // Log request
            await LogRequestAsync(context.Request, correlationId);

            // Capture response
            var originalResponseBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            finally
            {
                // Log response
                await LogResponseAsync(context.Response, correlationId);

                // Copy response back to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
        }

        private async Task LogRequestAsync(HttpRequest request, string correlationId)
        {
            try
            {
                request.EnableBuffering();

                var requestBody = string.Empty;
                if (request.ContentLength > 0 && request.ContentLength < 10000) // Only log reasonable sized bodies
                {
                    var buffer = new byte[Convert.ToInt32(request.ContentLength.Value)];
                    await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length));
                    requestBody = Encoding.UTF8.GetString(buffer);
                    request.Body.Seek(0, SeekOrigin.Begin);
                }

                var headers = GetSafeHeaders(request.Headers);

                Log.Information("REQUEST {@RequestInfo}", new
                {
                    CorrelationId = correlationId,
                    Method = request.Method,
                    Path = request.Path.Value,
                    QueryString = request.QueryString.Value,
                    Headers = headers,
                    Body = requestBody,
                    ContentType = request.ContentType,
                    ContentLength = request.ContentLength,
                    ClientIP = GetClientIP(request),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error logging request for correlation ID {CorrelationId}", correlationId);
            }
        }

        private async Task LogResponseAsync(HttpResponse response, string correlationId)
        {
            try
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);

                var headers = GetSafeHeaders(response.Headers);

                Log.Information("RESPONSE {@ResponseInfo}", new
                {
                    CorrelationId = correlationId,
                    StatusCode = response.StatusCode,
                    Headers = headers,
                    Body = responseBody.Length > 10000 ? $"[Body too large: {responseBody.Length} chars]" : responseBody,
                    ContentType = response.ContentType,
                    ContentLength = response.ContentLength,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error logging response for correlation ID {CorrelationId}", correlationId);
            }
        }

        private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
        {
            var safeHeaders = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                if (_sensitiveHeaders.Contains(header.Key))
                {
                    safeHeaders[header.Key] = "[REDACTED]";
                }
                else
                {
                    safeHeaders[header.Key] = string.Join(", ", header.Value.ToArray());
                }
            }
            return safeHeaders;
        }

        private string GetClientIP(HttpRequest request)
        {
            return request.Headers["X-Forwarded-For"].FirstOrDefault()
                   ?? request.Headers["X-Real-IP"].FirstOrDefault()
                   ?? request.HttpContext.Connection.RemoteIpAddress?.ToString()
                   ?? "Unknown";
        }

        private bool ShouldSkipLogging(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

            // Skip health checks, swagger, and static files
            return pathValue.Contains("/health") ||
                   pathValue.Contains("/swagger") ||
                   pathValue.Contains("/favicon") ||
                   pathValue.EndsWith(".css") ||
                   pathValue.EndsWith(".js") ||
                   pathValue.EndsWith(".map");
        }
    }

    // Extension method to register the middleware
    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}