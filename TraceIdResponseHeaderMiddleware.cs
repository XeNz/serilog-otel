using System.Diagnostics;
using Serilog.Context;

namespace SerilogTryOut;

public sealed class TraceIdResponseHeaderMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    [DebuggerStepThrough]
    public async Task Invoke(HttpContext context)
    {
        var traceId = Activity.Current!.TraceId.ToString();

        using (LogContext.PushProperty("TraceId", traceId))
        {
            context.Response.Headers.Append("TraceId", traceId);

            await _next(context);
        }
    }
}

public static class TraceIdResponseHeaderMiddlewareRegistration
{
    public static WebApplication UseTraceIdResponseHeader(this WebApplication builder)
    {
        builder.UseMiddleware<TraceIdResponseHeaderMiddleware>();

        return builder;
    }
}