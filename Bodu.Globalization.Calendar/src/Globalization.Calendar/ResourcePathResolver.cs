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
/// Backslash characters are accepted as input and normalised to <c>/</c>.
/// </para>
/// <para>
/// The resolver does not convert paths to embedded-resource names or file-system paths. Provider-specific mapping should
/// happen after resolution — <see cref="XmlResourceNotableDateRuleProvider" /> handles the final translation to a manifest
/// resource name by replacing <c>/</c> with <c>.</c>.
/// </para>
/// </remarks>
/// <example>
/// <para>Resolve relative and absolute child paths:</para>
/// <code>
/// IResourcePathResolver resolver = new ResourcePathResolver();
///
/// // Relative resolution — sibling document:
/// string result = resolver.Resolve(
///     "/Bodu/Globalization/Calendar/Resources/au-all.xml",
///     "../regions/au-nsw.xml");
/// // result: "/Bodu/Globalization/Calendar/regions/au-nsw.xml"
///
/// // Absolute child path (leading slash) — resolved as-is:
/// string abs = resolver.Resolve(
///     "/Bodu/Globalization/Calendar/Resources/au-all.xml",
///     "/Bodu/Globalization/Calendar/Resources/global-public.xml");
/// // abs: "/Bodu/Globalization/Calendar/Resources/global-public.xml"
/// </code>
/// </example>
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

    /// <summary>
    /// Replaces all backslash characters in <paramref name="path" /> with forward slashes.
    /// </summary>
    /// <param name="path">The path to normalise.</param>
    /// <returns>The path with backslashes replaced by forward slashes.</returns>
    private static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="path" /> begins with a forward slash and is therefore an absolute path.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><see langword="true" /> if the path is rooted; otherwise <see langword="false" />.</returns>
    private static bool IsRooted(string path)
    {
        return path.StartsWith("/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Collapses <c>.</c> and <c>..</c> segments from an absolute path and returns the canonical form.
    /// </summary>
    /// <param name="path">The absolute path to normalise. Need not already begin with <c>/</c>.</param>
    /// <returns>The normalised absolute path, always starting with <c>/</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <c>..</c> traversal escapes the root.</exception>
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
                        string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_ResourcePathEscapesRoot, path));

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return "/" + string.Join('/', segments);
    }

    /// <summary>
    /// Returns the parent directory segment of <paramref name="documentPath" /> after normalising it to an absolute path.
    /// </summary>
    /// <param name="documentPath">The fully qualified resource path of a document.</param>
    /// <returns>The parent directory path, or <c>/</c> when the document is at the root.</returns>
    private static string GetParentDirectory(string documentPath)
    {
        documentPath = NormalizeAbsolute(documentPath);

        var lastSlash = documentPath.LastIndexOf('/');

        return lastSlash <= 0
            ? "/"
            : documentPath[..lastSlash];
    }

    /// <summary>
    /// Joins <paramref name="parentDirectory" /> and <paramref name="childPath" /> with a single forward slash separator.
    /// </summary>
    /// <param name="parentDirectory">The parent directory path.</param>
    /// <param name="childPath">The relative child path to append.</param>
    /// <returns>The combined path.</returns>
    private static string Combine(string parentDirectory, string childPath)
    {
        if (string.IsNullOrWhiteSpace(parentDirectory) || parentDirectory == "/")
            return "/" + childPath.TrimStart('/');

        return parentDirectory.TrimEnd('/') + "/" + childPath.TrimStart('/');
    }
}