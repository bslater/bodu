// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolvedEntry.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Carries the origin metadata for a single key/value pair in a <see cref="ConfigurationView" /> — which
/// section "won" for the key, which line of which file supplied the value, and the resolved key path itself.
/// </summary>
/// <remarks>
/// <para>
/// Resolved entries are the debugging surface for layered configuration: when a host has loaded several
/// matching sections and a preamble, and a key resolves to an unexpected value, the entry's
/// <see cref="SectionPattern" /> identifies the section that won under last-wins precedence and
/// <see cref="SourceLocation" /> points back to the originating line.
/// </para>
/// <para>
/// The values exposed here are a snapshot of the resolved view. Mutation of the originating document after
/// resolution does not propagate into existing entries.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// ConfigurationView view = doc.Resolve("src/Foo.cs");
/// ConfigurationResolvedEntry? entry = view.GetEntry("format:indent:size");
/// if (entry is not null)
/// {
///     Console.WriteLine($"{entry.Key} = {entry.Value}");
///     Console.WriteLine($"  from section: {entry.SectionPattern ?? "<preamble>"}");
///     Console.WriteLine($"  at: {entry.SourceLocation}");
/// }
///]]>
/// </example>
public sealed class ConfigurationResolvedEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationResolvedEntry" /> class.
    /// </summary>
    /// <param name="key">The canonical configuration key in colon-delimited form.</param>
    /// <param name="value">The resolved value, or <see langword="null" /> when the source value was null.</param>
    /// <param name="sourceLocation">The position in the source document that supplied the value.</param>
    /// <param name="sectionPattern">
    /// The pattern of the section that supplied the value, or <see langword="null" /> when the value came
    /// from the document preamble (global section).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public ConfigurationResolvedEntry(
        string key,
        string? value,
        ConfigurationSourceLocation sourceLocation,
        string? sectionPattern)
    {
        ThrowHelper.ThrowIfNull(key);

        Key = key;
        Value = value;
        SourceLocation = sourceLocation;
        SectionPattern = sectionPattern;
    }

    /// <summary>
    /// Gets the canonical configuration key (colon-delimited).
    /// </summary>
    /// <returns>The key as it appears in <see cref="ConfigurationView.Values" />.</returns>
    public string Key { get; }

    /// <summary>
    /// Gets the resolved value for <see cref="Key" />.
    /// </summary>
    /// <returns>The value, or <see langword="null" /> when the source value was null.</returns>
    public string? Value { get; }

    /// <summary>
    /// Gets the position in the source document that supplied the value.
    /// </summary>
    /// <returns>
    /// The source location. Only the <see cref="ConfigurationSourceLocation.LineNumber" /> is reliably
    /// populated — line position and length are approximate and the document path is propagated only when
    /// the document was loaded from a file.
    /// </returns>
    public ConfigurationSourceLocation SourceLocation { get; }

    /// <summary>
    /// Gets the pattern of the section that supplied the value, or <see langword="null" /> when the value
    /// came from the document preamble (global section).
    /// </summary>
    /// <returns>The section pattern (e.g. <c>*.cs</c>), or <see langword="null" /> for preamble entries.</returns>
    public string? SectionPattern { get; }
}
