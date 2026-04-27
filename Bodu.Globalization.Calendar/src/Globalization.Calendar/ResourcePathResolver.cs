// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcePathResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves resource references relative to the resource document that declares them.
/// </summary>
/// <remarks>
/// <para>
/// Paths are resolved using a logical path model where <c>/</c> is the canonical separator.
/// Backslashes are accepted as input and normalized to <c>/</c>.
/// </para>
/// <para>
/// The resolver does not convert paths to embedded-resource names or file-system paths. Provider-specific mapping should
/// happen after resolution.
/// </para>
/// </remarks>
public sealed class ResourcePathResolver : IResourcePathResolver
{
    /// <inheritdoc />
    public string Resolve(string documentPath, string childPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(childPath);

        documentPath = Normalize(documentPath.Trim());
        childPath = Normalize(childPath.Trim());

        if (IsRooted(childPath))
            return NormalizeAbsolute(childPath);

        var parentDirectory = GetParentDirectory(documentPath);

        return NormalizeAbsolute(Combine(parentDirectory, childPath));
    }

    private static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool IsRooted(string path)
    {
        return path.StartsWith("/", StringComparison.Ordinal);
    }

    private static string NormalizeAbsolute(string path)
    {
        var segments = new List<string>();

        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (segments.Count == 0)
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_ResourcePathEscapesRoot, path));

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return "/" + string.Join('/', segments);
    }

    private static string GetParentDirectory(string documentPath)
    {
        documentPath = NormalizeAbsolute(documentPath);

        var lastSlash = documentPath.LastIndexOf('/');

        return lastSlash <= 0
            ? "/"
            : documentPath[..lastSlash];
    }

    private static string Combine(string parentDirectory, string childPath)
    {
        if (string.IsNullOrWhiteSpace(parentDirectory) || parentDirectory == "/")
            return "/" + childPath.TrimStart('/');

        return parentDirectory.TrimEnd('/') + "/" + childPath.TrimStart('/');
    }
}