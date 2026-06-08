// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Projects an <see cref="IniDocumentBase" /> into a flattened <see cref="ConfigurationView" /> for a specific target path,
/// applying glob matching, preamble layering, and the EditorConfig <c>unset</c> sentinel.
/// </summary>
internal sealed class ConfigurationResolver
{
    /// <summary>
    /// The EditorConfig sentinel value that removes a previously set key from the effective configuration. Compared
    /// case-insensitively to match real-world EditorConfig tooling — a deliberate deviation from the strict
    /// lower-case-only reading of the spec.
    /// </summary>
    private const string UnsetSentinel = "unset";

    private readonly ConfigurationResolveOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationResolver" /> class with the supplied resolve options.
    /// </summary>
    /// <param name="options">The resolve options that govern glob matching and preamble layering.</param>
    internal ConfigurationResolver(ConfigurationResolveOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Projects <paramref name="document" /> into a resolved <see cref="ConfigurationView" /> for
    /// <paramref name="targetPath" />.
    /// </summary>
    /// <param name="document">The document to resolve.</param>
    /// <param name="targetPath">The target path the view is evaluated for, or <see langword="null" />.</param>
    /// <returns>The resolved view for <paramref name="targetPath" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The configured options require a path root, the document was parsed without one, and no target path was
    /// supplied.
    /// </exception>
    internal ConfigurationView Resolve(IniDocumentBase document, string? targetPath)
    {
        ThrowHelper.ThrowIfNull(document);

        var pathRoot = _options.PathRoot;
        if (pathRoot is null && _options.MissingPathRootMode == ConfigurationMissingPathRootMode.Throw && targetPath is null)
            ConfigurationHelpers.ThrowResolveWithoutPathRoot();

        StringComparer comparer = _options.KeyOptions.KeyComparer;
        Dictionary<string, string?> values = new(comparer);
        Dictionary<string, ConfigurationResolvedEntry> entries = new(comparer);

        var normalizedTarget = targetPath is null ? string.Empty : NormalizePath(targetPath, pathRoot);

        // Apply the global section (preamble) first when enabled. Preamble entries are signalled by passing
        // a null section pattern through to ApplySection.
        if (_options.ApplyPreambleProperties)
            ApplySection(document.GlobalSection, values, entries, sectionPattern: null);

        // Apply sections whose name (interpreted as a glob pattern) matches the target path, in source order.
        // Last-wins precedence is naturally handled by dictionary overwrite. The configured PathComparison
        // controls case sensitivity of the underlying regex match.
        foreach (IniSection section in document.Sections)
        {
            if (string.IsNullOrEmpty(normalizedTarget))
                continue;

            bool matched;
            try
            {
                matched = ConfigurationPattern.Compile(section.Name, _options.PathComparison).IsMatch(normalizedTarget);
            }
            catch (RegexMatchTimeoutException)
            {
                // With NonBacktracking this is effectively unreachable; treat a timeout as a non-match so a
                // pathological section name cannot abort resolution.
                matched = false;
            }

            if (matched)
                ApplySection(section, values, entries, sectionPattern: section.Name);
        }

        return new ConfigurationView(values, entries);
    }

    /// <summary>
    /// Applies the entries of <paramref name="section" /> to <paramref name="values" /> and <paramref name="entries" />
    /// , honoring the EditorConfig <c>unset</c> sentinel.
    /// </summary>
    /// <param name="section">The section whose entries are layered onto the resolved values.</param>
    /// <param name="values">The accumulating dictionary of resolved values.</param>
    /// <param name="entries">The accumulating dictionary of origin metadata per key.</param>
    /// <param name="sectionPattern">
    /// The section pattern, or <see langword="null" /> when <paramref name="section" /> is the preamble.
    /// </param>
    private void ApplySection(
        IniSection section,
        Dictionary<string, string?> values,
        Dictionary<string, ConfigurationResolvedEntry> entries,
        string? sectionPattern)
    {
        foreach (IniEntry entry in section.Entries)
        {
            var key = ConfigurationKey.Parse(entry.Key, _options.KeyOptions).Path;

            // EditorConfig "unset" sentinel handling.
            if (_options.UnsetValueMode == ConfigurationUnsetValueMode.RemoveEffectiveValue
                && string.Equals(entry.Value, UnsetSentinel, StringComparison.OrdinalIgnoreCase))
            {
                values.Remove(key);
                entries.Remove(key);
                continue;
            }

            values[key] = entry.Value;
            entries[key] = new ConfigurationResolvedEntry(
                key,
                entry.Value,
                new ConfigurationSourceLocation(entry.LineNumber, linePosition: 1, length: entry.Key.Length),
                sectionPattern);
        }
    }

    /// <summary>
    /// Normalizes <paramref name="targetPath" /> to forward slashes and rebases it relative to
    /// <paramref name="pathRoot" />.
    /// </summary>
    /// <param name="targetPath">The target path to normalize.</param>
    /// <param name="pathRoot">The path root the result is made relative to, or <see langword="null" />.</param>
    /// <returns>The normalized, root-relative path.</returns>
    private static string NormalizePath(string targetPath, string? pathRoot)
    {
        var normalizedTarget = targetPath.Replace('\\', '/');
        if (string.IsNullOrEmpty(pathRoot))
            return normalizedTarget;

        var normalizedRoot = pathRoot.Replace('\\', '/').TrimEnd('/');
        return normalizedTarget.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase)
            ? normalizedTarget[(normalizedRoot.Length + 1)..]
            : string.Equals(normalizedTarget, normalizedRoot, StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(normalizedTarget)
                : normalizedTarget;
    }
}
