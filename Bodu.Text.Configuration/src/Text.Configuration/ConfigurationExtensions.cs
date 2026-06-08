// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Extension methods that layer Bodu Text Configuration behaviour (path-aware resolution and dotted-to-colon key
/// mapping) onto the underlying <see cref="IniDocumentBase" /> and <see cref="IniEntry" /> primitives.
/// </summary>
/// <remarks>
/// <para>
/// The Bodu Text Configuration model is intentionally layered on top of the raw INI primitives from
/// <c>Bodu.Text.Ini</c> rather than replacing them: an <see cref="IniDocumentBase" /> remains the source-faithful
/// in-memory representation, and these extension methods add the configuration-specific behaviour — target-path
/// resolution and dotted-to-colon key normalization — that turns that raw document into the resolved snapshot consumed
/// by application code.
/// </para>
/// <para>
/// The primary entry point is <see cref="Resolve(IniDocumentBase, string?, ConfigurationResolveOptions?)" />, which
/// produces a <see cref="ConfigurationView" /> for a supplied target path. Pair it with
/// <see cref="ConfigurationDocument.Parse(string)" /> at the start of the pipeline and with the typed accessors on
/// <see cref="ConfigurationView" /> at its end.
/// </para>
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Projects the document into a resolved <see cref="ConfigurationView" /> for the supplied target path using the
    /// default Bodu resolve options.
    /// </summary>
    /// <param name="document">The document to resolve.</param>
    /// <param name="targetPath">The path the resolved view is evaluated for, or <see langword="null" />.</param>
    /// <returns>A populated <see cref="ConfigurationView" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    public static ConfigurationView Resolve(this IniDocumentBase document, string? targetPath = null) =>
        document.Resolve(targetPath, options: null);

    /// <summary>
    /// Projects the document into a resolved <see cref="ConfigurationView" /> for the supplied target path using the
    /// supplied options.
    /// </summary>
    /// <param name="document">The document to resolve.</param>
    /// <param name="targetPath">The path the resolved view is evaluated for, or <see langword="null" />.</param>
    /// <param name="options">The resolve options, or <see langword="null" /> for the Bodu defaults.</param>
    /// <returns>A populated <see cref="ConfigurationView" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The supplied options require a path root and none was provided.
    /// </exception>
    public static ConfigurationView Resolve(this IniDocumentBase document, string? targetPath, ConfigurationResolveOptions? options) =>
        new ConfigurationResolver(options ?? ConfigurationResolveOptions.Bodu).Resolve(document, targetPath);

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
}
