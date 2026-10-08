// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Extension methods that layer Bodu Text Configuration behaviour (path-aware resolution and dotted-to-colon key
/// mapping) onto the underlying <see cref="IniDocumentBase" /> and <see cref="IniEntry" /> primitives.
/// </summary>
/// <remarks>
/// <para>
/// The Bodu Text Configuration model is intentionally layered on top of its raw INI document primitives rather than
/// replacing them: an <see cref="IniDocumentBase" /> remains the source-faithful in-memory representation, and these
/// extension methods add the configuration-specific behaviour - target-path resolution and dotted-to-colon key
/// normalization - that turns that raw document into the resolved snapshot consumed by application code.
/// </para>
/// <para>
/// The primary entry point is <see cref="Resolve(IniDocumentBase, string?, ConfigurationResolveOptions?)" />, which
/// produces a <see cref="ConfigurationView" /> for a supplied target path. Pair it with
/// <see cref="ConfigurationDocument.Parse(string)" /> at the start of the pipeline and with the typed accessors on
/// <see cref="ConfigurationView" /> at its end.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// ConfigurationDocument document = ConfigurationDocument.Parse("""
///     root = true
///
///     [*]
///     indent_size = 4
///
///     [src/**.cs]
///     indent_size = 8
///     """);
///
/// // Sections whose glob matches the target path apply, later sections winning.
/// ConfigurationView view = document.Resolve("src/App/Program.cs");
/// var indent = view.GetInt32("indent_size");   // 8
///]]>
/// </code>
/// </example>
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
    /// <remarks>
    /// A document loaded with <see cref="ConfigurationDocument.Load(string, ConfigurationParseOptions?)" /> is rooted
    /// at the directory of its file, so a target path under that directory is matched relative to it.
    /// </remarks>
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
    /// <paramref name="targetPath" /> is <see langword="null" />, <paramref name="options" /> selects
    /// <see cref="ConfigurationMissingPathRootMode.Throw" />, and no root is known: <paramref name="options" /> sets no
    /// <see cref="ConfigurationResolveOptions.PathRoot" /> and the document was not loaded from a file.
    /// </exception>
    /// <remarks>
    /// When <paramref name="options" /> sets no <see cref="ConfigurationResolveOptions.PathRoot" />, a document loaded
    /// with <see cref="ConfigurationDocument.Load(string, ConfigurationParseOptions?)" /> is rooted at the directory of
    /// its file, so a target path under that directory is matched relative to it.
    /// </remarks>
    public static ConfigurationView Resolve(this IniDocumentBase document, string? targetPath, ConfigurationResolveOptions? options) =>
        new ConfigurationResolver(options ?? ConfigurationResolveOptions.Bodu).Resolve(document, targetPath);

    /// <summary>
    /// Computes the configuration path for an entry's raw key using the supplied key options: colon-delimited, or the
    /// key as written under <see cref="ConfigurationKeyMapping.Identity" />. Mirrors
    /// <see cref="ConfigurationKey.Parse(string, ConfigurationKeyOptions?)" /> and exposes the resulting
    /// <see cref="ConfigurationKey.Path" />.
    /// </summary>
    /// <param name="entry">The entry whose key should be transformed.</param>
    /// <param name="options">The key options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The configuration path of the entry's key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entry" /> is <see langword="null" />.</exception>
    public static string ConfigurationPath(this IniEntry entry, ConfigurationKeyOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(entry);

        return ConfigurationKey.Parse(entry.Key, options).Path;
    }
}
