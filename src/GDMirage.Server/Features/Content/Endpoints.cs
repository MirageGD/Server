using GDMirage.Server.Configuration;
using Microsoft.Extensions.Options;

namespace GDMirage.Server.Features.Content;

public static partial class Endpoints
{
    public static void MapContentEndpoint(this WebApplication app)
    {
        app.MapGet("/content/{**path}", GetContent);
    }

    private static IResult GetContent(string path, HttpContext context, ILogger<Program> logger, IOptions<ServerOptions> options)
    {
        if (path.Contains("..") || Path.IsPathRooted(path))
        {
            logger.LogRejectedPathTraversalAttempt(path);

            return Results.BadRequest("Invalid filename.");
        }

        path = Path.Combine(options.Value.ContentRoot, path);
        if (!File.Exists(path))
        {
            logger.LogFileNotFound(path);

            return Results.NotFound();
        }

        var lastWrite = File.GetLastWriteTimeUtc(path);
        var lastWriteTicks = lastWrite.Ticks;

        var etag = $"{lastWriteTicks:x}";

        var ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault();
        if (ifNoneMatch == etag)
        {
            logger.LogFileNotModified(path);

            return Results.StatusCode(304);
        }

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".tmj" or ".tsj" or ".json" => "application/json",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        context.Response.Headers.ETag = etag;

        logger.LogServing(path);

        return Results.Stream(File.OpenRead(path), contentType);
    }

    [LoggerMessage(LogLevel.Warning, "Rejected path traversal attempt: {fileName}")]
    static partial void LogRejectedPathTraversalAttempt(this ILogger<Program> logger, string fileName);

    [LoggerMessage(LogLevel.Information, "File not found: {path}")]
    static partial void LogFileNotFound(this ILogger<Program> logger, string path);

    [LoggerMessage(LogLevel.Debug, "File not modified: {path}")]
    static partial void LogFileNotModified(this ILogger<Program> logger, string path);

    [LoggerMessage(LogLevel.Debug, "Serving {path}")]
    static partial void LogServing(this ILogger<Program> logger, string path);
}
