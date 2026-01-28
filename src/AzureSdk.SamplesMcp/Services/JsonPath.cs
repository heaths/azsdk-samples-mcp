using System.Text.Json;

namespace AzureSdk.SamplesMcp.Services;

/// <summary>
/// Simple JSONPath query utility for basic path traversal and array enumeration.
/// </summary>
/// <remarks>
/// Supports:
/// - Root access: "."
/// - Property access: ".property"
/// - Array enumeration: ".property[]"
/// - Nested paths: ".property1.property2[]"
/// - Object key access: ".property.key"
/// Does not support filtering or complex expressions.
/// </remarks>
internal static class JsonPath
{
    /// <summary>
    /// Query a JSON document using a simple path expression.
    /// </summary>
    /// <param name="document">The JSON document to query.</param>
    /// <param name="path">The path expression (e.g., ".packages[]", ".projects[].frameworks[].topLevelPackages[]").</param>
    /// <returns>An enumerable of matching JsonElements.</returns>
    public static IEnumerable<JsonElement> Query(JsonDocument document, string path)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Query(document.RootElement, path);
    }

    /// <summary>
    /// Query a JsonElement using a simple path expression.
    /// </summary>
    /// <param name="element">The JsonElement to query.</param>
    /// <param name="path">The path expression (e.g., ".packages[]", ".projects[].frameworks[].topLevelPackages[]").</param>
    /// <returns>An enumerable of matching JsonElements.</returns>
    public static IEnumerable<JsonElement> Query(JsonElement element, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Remove leading dot if present
        if (path.StartsWith('.'))
        {
            path = path[1..];
        }

        // Handle root-only path
        if (string.IsNullOrEmpty(path))
        {
            yield return element;
            yield break;
        }

        // Parse and execute the path
        foreach (var result in ExecutePath(element, path))
        {
            yield return result;
        }
    }

    private static IEnumerable<JsonElement> ExecutePath(JsonElement element, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            yield return element;
            yield break;
        }

        // Handle direct array enumeration at current level (e.g., "[]" or "[].property")
        if (path.StartsWith("[]"))
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var nextPath = path.Length > 2 && path[2] == '.' ? path[3..] : path.Length > 2 ? path[2..] : string.Empty;
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var result in ExecutePath(item, nextPath))
                    {
                        yield return result;
                    }
                }
            }
            yield break;
        }

        // Find the next segment separator (either . or [)
        var nextDot = path.IndexOf('.');
        var nextBracket = path.IndexOf('[');

        int separatorIndex;
        if (nextDot == -1 && nextBracket == -1)
        {
            // No more separators - this is the last segment
            separatorIndex = path.Length;
        }
        else if (nextDot == -1)
        {
            separatorIndex = nextBracket;
        }
        else if (nextBracket == -1)
        {
            separatorIndex = nextDot;
        }
        else
        {
            separatorIndex = Math.Min(nextDot, nextBracket);
        }

        var segment = path[..separatorIndex];
        var remainder = separatorIndex < path.Length ? path[separatorIndex..] : string.Empty;

        // Handle array enumeration after property
        if (remainder.StartsWith("[]"))
        {
            // Get the property and enumerate its array
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(segment, out var arrayElement))
            {
                if (arrayElement.ValueKind == JsonValueKind.Array)
                {
                    var nextPath = remainder.Length > 2 && remainder[2] == '.' ? remainder[3..] : remainder.Length > 2 ? remainder[2..] : string.Empty;
                    foreach (var item in arrayElement.EnumerateArray())
                    {
                        foreach (var result in ExecutePath(item, nextPath))
                        {
                            yield return result;
                        }
                    }
                }
            }
        }
        // Handle property access
        else if (remainder.StartsWith(".") || string.IsNullOrEmpty(remainder))
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(segment, out var propertyElement))
            {
                var nextPath = remainder.StartsWith(".") ? remainder[1..] : string.Empty;
                foreach (var result in ExecutePath(propertyElement, nextPath))
                {
                    yield return result;
                }
            }
        }
    }
}
