// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Extension methods that layer Bodu Text Configuration behaviour (glob matching, path-aware resolution,
/// dotted-to-colon key mapping) onto the underlying <see cref="IniDocument" />, <see cref="IniSection" />, and
/// <see cref="IniEntry" /> primitives.
/// </summary>
/// <remarks>
/// <para>
/// The Bodu Text Configuration model is intentionally layered on top of the raw INI primitives from
/// <c>Bodu.Text.Ini</c> rather than replacing them: an <see cref="IniDocument" /> remains the source-faithful
/// in-memory representation, and these extension methods add the configuration-specific behaviour — section glob
/// matching, target-path resolution, dotted-to-colon key normalization, and preamble layering — that turns that raw
/// document into the resolved snapshot consumed by application code.
/// </para>
/// <para>
/// The primary entry point is <see cref="Resolve(IniDocument, string?, ConfigurationResolveOptions?)" />, which
/// produces a <see cref="ConfigurationView" /> for a supplied target path. Pair it with
/// <see cref="ConfigurationDocument.Parse(string)" /> at the start of the pipeline and with the typed accessors on
/// <see cref="ConfigurationView" /> at its end.
/// </para>
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Projects the document into a resolved <see cref="ConfigurationView" /> for the supplied target path using
    /// the default Bodu resolve options.
    /// </summary>
    /// <param name="document">The document to resolve.</param>
    /// <param name="targetPath">The path the resolved view is evaluated for, or <see langword="null" />.</param>
    /// <returns>A populated <see cref="ConfigurationView" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    public static ConfigurationView Resolve(this IniDocument document, string? targetPath = null) =>
        document.Resolve(targetPath, options: null);

    /// <summary>
    /// Projects the document into a resolved <see cref="ConfigurationView" /> for the supplied target path using
    /// the supplied options.
    /// </summary>
    /// <param name="document">The document to resolve.</param>
    /// <param name="targetPath">The path the resolved view is evaluated for, or <see langword="null" />.</param>
    /// <param name="options">The resolve options, or <see langword="null" /> for the Bodu defaults.</param>
    /// <returns>A populated <see cref="ConfigurationView" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The supplied options require a path root and none was provided.
    /// </exception>
    public static ConfigurationView Resolve(this IniDocument document, string? targetPath, ConfigurationResolveOptions? options) =>
        new ConfigurationResolver(options ?? ConfigurationResolveOptions.Bodu).Resolve(document, targetPath);

    /// <summary>
    /// Determines whether the supplied target path matches the section's name interpreted as a glob pattern.
    /// </summary>
    /// <param name="section">The section whose name should be treated as a glob pattern.</param>
    /// <param name="relativePath">The path to test, relative to the document's path root.</param>
    /// <returns>
    /// <see langword="true" /> when the path matches; otherwise, <see langword="false" />. The global section (whose
    /// <see cref="IniSection.Name" /> is empty) always returns <see langword="false" />; it is layered separately by
    /// the resolver.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="section" /> or <paramref name="relativePath" /> is <see langword="null" />.
    /// </exception>
    public static bool IsMatch(this IniSection section, string relativePath)
    {
        ThrowHelper.ThrowIfNull(section);
        ThrowHelper.ThrowIfNull(relativePath);

        return section.Name.Length != 0 && ConfigurationPattern.Compile(section.Name).IsMatch(relativePath);
    }

    /// <summary>
    /// Computes the colon-delimited configuration path for an entry's raw key using the supplied key options. Mirrors
    /// <see cref="ConfigurationKey.Parse(string, ConfigurationKeyOptions?)" /> and exposes the resulting
    /// <see cref="ConfigurationKey.Path" />.
    /// </summary>
    /// <param name="entry">The entry whose key should be transformed.</param>
    /// <param name="options">The key options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The colon-delimited configuration path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entry" /> is <see langword="null" />.</exception>
    public static string ConfigurationPath(this IniEntry entry, ConfigurationKeyOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(entry);

        return ConfigurationKey.Parse(entry.Key, options).Path;
    }

    /// <summary>
    /// Convenience alias for <see cref="IniDocument.GlobalSection" /> — returns the same instance under the "preamble"
    /// name familiar from EditorConfig terminology.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>The global / preamble section.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    public static IniSection Preamble(this IniDocument document)
    {
        ThrowHelper.ThrowIfNull(document);

        return document.GlobalSection;
    }
}
