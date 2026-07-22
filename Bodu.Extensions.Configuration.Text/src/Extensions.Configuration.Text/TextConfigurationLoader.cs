// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationLoader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Internal helper that owns the Parse → Resolve → flatten pipeline shared between
/// <see cref="TextConfigurationProvider" /> (file-backed) and <see cref="TextStreamConfigurationProvider" />
/// (stream-backed). Keeps the two providers' <c>Load</c> implementations in lock-step.
/// </summary>
/// <remarks>
/// <para>
/// Projection from <see cref="ConfigurationView" /> into
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration" /> is intentionally lossy. The richer document model
/// preserves information that does not survive the flatten step:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Leading and inline comments are kept on the parsed document but discarded here.</description>
/// </item>
/// <item>
/// <description>
/// Source locations (line/column/path) are kept on the parsed document and on every diagnostic but are not projected
/// onto the flattened keys.
/// </description>
/// </item>
/// <item>
/// <description>
/// Duplicate-section provenance — which matching section "won" for a given key — is collapsed by the resolver's
/// last-wins precedence and is not exposed.
/// </description>
/// </item>
/// <item>
/// <description>
/// Key case is normalised: the output dictionary uses <see cref="StringComparer.OrdinalIgnoreCase" />, matching the
/// Microsoft conventions.
/// </description>
/// </item>
/// <item>
/// <description>
/// Literal colons inside key segments cannot survive — the colon is the hierarchy delimiter in
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration" />, so a key like <c>service\:name</c> in the source
/// document is split into the hierarchy <c>service</c> / <c>name</c> by the configuration system once flattened.
/// </description>
/// </item>
/// </list>
/// <para>
/// Consumers who need any of the discarded metadata should depend on <see cref="ConfigurationDocument" /> directly
/// rather than going through the Microsoft provider bridge.
/// </para>
/// </remarks>
internal static class TextConfigurationLoader
{
    /// <summary>
    /// Parses <paramref name="stream" /> as a Bodu Text Configuration document, resolves it against the supplied
    /// <paramref name="targetPath" />, and returns the flattened colon-delimited key/value map ready to assign to
    /// <see cref="Microsoft.Extensions.Configuration.ConfigurationProvider.Data" />.
    /// </summary>
    /// <param name="stream">The configuration source stream. The stream is left open.</param>
    /// <param name="targetPath">The optional target path used to evaluate glob-anchored sections.</param>
    /// <param name="parseOptions">
    /// The parse options, or <see langword="null" /> for <see cref="ConfigurationParseOptions.Bodu" />.
    /// </param>
    /// <param name="resolveOptions">
    /// The resolve options, or <see langword="null" /> for <see cref="ConfigurationResolveOptions.Bodu" />.
    /// </param>
    /// <returns>A case-insensitive dictionary with colon-delimited logical keys.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    internal static IDictionary<string, string?> LoadData(
        Stream stream,
        string? targetPath,
        ConfigurationParseOptions? parseOptions,
        ConfigurationResolveOptions? resolveOptions)
    {
        ThrowHelper.ThrowIfNull(stream);

        ConfigurationParseOptions effectiveParse = parseOptions ?? ConfigurationParseOptions.Bodu;
        ConfigurationResolveOptions effectiveResolve = resolveOptions ?? ConfigurationResolveOptions.Bodu;

        var document = ConfigurationDocument.Load(stream, effectiveParse, leaveOpen: true);

        return LoadData(document, targetPath, effectiveResolve);
    }

    /// <summary>
    /// Resolves an already-parsed <paramref name="document" /> against the supplied <paramref name="targetPath" /> and
    /// returns the flattened colon-delimited key/value map.
    /// </summary>
    /// <param name="document">The parsed configuration document.</param>
    /// <param name="targetPath">The optional target path used to evaluate glob-anchored sections.</param>
    /// <param name="resolveOptions">
    /// The resolve options, or <see langword="null" /> for <see cref="ConfigurationResolveOptions.Bodu" />.
    /// </param>
    /// <returns>A case-insensitive dictionary with colon-delimited logical keys.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    internal static IDictionary<string, string?> LoadData(
        IniDocumentBase document,
        string? targetPath,
        ConfigurationResolveOptions? resolveOptions)
    {
        ThrowHelper.ThrowIfNull(document);

        ConfigurationResolveOptions effectiveResolve = resolveOptions ?? ConfigurationResolveOptions.Bodu;
        ConfigurationView view = document.Resolve(targetPath, effectiveResolve);

        Dictionary<string, string?> data = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string?> entry in view)
            data[entry.Key] = entry.Value;

        return data;
    }
}
