using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace stockmind.Middlewares;

public class RequestResponseLoggingMiddleware
{
    private const int MaxBodyLength = 4096;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestTime = DateTime.UtcNow;
        var requestBody = await ReadRequestBodyAsync(context.Request);

        _logger.LogInformation(
            "Incoming request {Method} {Path} | Query: {QueryString} | Body: {Body}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString.Value,
            requestBody);

        await using var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        await _next(context);

        var responseBody = await ReadResponseBodyAsync(context.Response);
        var elapsed = DateTime.UtcNow - requestTime;

        _logger.LogInformation(
            "Outgoing response {StatusCode} for {Method} {Path} in {ElapsedMilliseconds} ms | Body: {Body}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path,
            elapsed.TotalMilliseconds,
            responseBody);

        responseBuffer.Seek(0, SeekOrigin.Begin);
        await responseBuffer.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0 || !request.Body.CanRead)
        {
            return string.Empty;
        }

        request.EnableBuffering();

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var content = await ReadLimitedAsync(reader);
        request.Body.Seek(0, SeekOrigin.Begin);

        return content;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var content = await ReadLimitedAsync(reader);

        response.Body.Seek(0, SeekOrigin.Begin);
        return content;
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader)
    {
        var buffer = new char[Math.Min(MaxBodyLength, 2048)];
        var builder = new StringBuilder();
        int totalRead = 0;
        int read;

        while ((read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0 && totalRead < MaxBodyLength)
        {
            var toAppend = read;
            if (totalRead + read > MaxBodyLength)
            {
                toAppend = MaxBodyLength - totalRead;
            }

            builder.Append(buffer, 0, toAppend);
            totalRead += toAppend;

            if (totalRead >= MaxBodyLength)
            {
                builder.Append("...<truncated>");
                break;
            }
        }

        return builder.ToString();
    }
}
