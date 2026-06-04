using LogDashboard.Web.Models;
using System.Text.Json;

namespace LogDashboard.Web.Services;

public class LogParserService
{
    private readonly ILogger<LogParserService> _logger;

    public LogParserService(ILogger<LogParserService> logger)
    {
        _logger = logger;
    }

    public async Task<List<LogEntry>> ParseFolderAsync(string folderPath, bool includeSubDirectories = true)
    {
        var result = new List<LogEntry>();

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Folder not found: {Path}", folderPath);
            return result;
        }

        var searchOption = includeSubDirectories
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = Directory.GetFiles(folderPath, "*.json", searchOption);

        var tasks = files.Select(f => ParseFileAsync(f));
        var results = await Task.WhenAll(tasks);

        foreach (var entries in results)
            result.AddRange(entries);

        return result;
    }

    public async Task<List<LogEntry>> ParseFileAsync(string filePath)
    {
        var result = new List<LogEntry>();

        if (!File.Exists(filePath)) return result;

        try
        {
            using var stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                try
                {
                    var entry = ParseLine(trimmed);
                    if (entry is not null) result.Add(entry);
                }
                catch
                {
                    // skip unparseable lines
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read file: {Path}", filePath);
        }

        return result;
    }

    private static LogEntry? ParseLine(string line)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        var timestamp = root.TryGetProperty("@t", out var t)
            ? DateTime.Parse(t.GetString()!).ToLocalTime()
            : DateTime.MinValue;

        var level = root.TryGetProperty("@l", out var l)
            ? l.GetString() ?? "Information"
            : "Information";

        var message = root.TryGetProperty("@m", out var m)
            ? m.GetString() ?? string.Empty
            : root.TryGetProperty("@mt", out var mt)
                ? mt.GetString() ?? string.Empty
                : string.Empty;

        var exception = root.TryGetProperty("@x", out var x)
            ? x.GetString()
            : null;

        var properties = new Dictionary<string, object?>();
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name.StartsWith('@')) continue;

            properties[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => prop.Value.GetRawText()
            };
        }

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message,
            Exception = exception,
            Properties = properties
        };
    }
}
