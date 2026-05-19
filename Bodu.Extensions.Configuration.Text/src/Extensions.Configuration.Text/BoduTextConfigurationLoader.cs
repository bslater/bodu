// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationLoader.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;
using Bodu.Text.Ini;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Internal helper that owns the Parse → Resolve → flatten pipeline shared between
/// <see cref="BoduTextConfigurationProvider" /> (file-backed) and <see cref="BoduTextStreamConfigurationProvider" />
/// (stream-backed). Keeps the two providers' <c>Load</c> implementations in lock-step.
/// </summary>
internal static class BoduTextConfigurationLoader
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

        IniDocument document = ConfigurationDocument.Load(stream, effectiveParse, leaveOpen: true);

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
        IniDocument document,
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
