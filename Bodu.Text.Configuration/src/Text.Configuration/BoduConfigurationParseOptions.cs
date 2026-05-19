// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how a configuration document is parsed: comment handling, duplicate handling, diagnostic routing, length
/// limits, and the key mapping options.
/// </summary>
/// <remarks>
/// <para>
/// The options bag is a profile-grouped set of <c>init</c>-only properties. Start from a named preset that matches the
/// hosting flavour — <see cref="Bodu" /> for the default Bodu Text Configuration semantics,
/// <see cref="EditorConfigCompatible" /> for strict EditorConfig parity, <see cref="Strict" /> for fail-fast behaviour,
/// or <see cref="Relaxed" /> for lenient parsing — and override the specific properties that need to differ. Use
/// <see cref="For(BoduConfigurationProfile)" /> when the profile is data-driven (for example, read from configuration).
/// </para>
/// <para>
/// <see cref="DiagnosticMode" /> is the most consequential knob. In
/// <see cref="BoduConfigurationDiagnosticMode.Throw" /> the reader raises
/// <see cref="BoduConfigurationParseException" /> on the first recoverable error; in
/// <see cref="BoduConfigurationDiagnosticMode.Collect" /> the parser drives through the document and reports every
/// recoverable issue via the parse result's <see cref="BoduConfigurationParseResult.Diagnostics" /> list. Pair the
/// latter with <see cref="BoduConfigurationDocument.ParseWithDiagnostics(string, BoduConfigurationParseOptions?)" />.
/// </para>
/// <para>
/// Key shape is delegated to <see cref="KeyOptions" />. Sharing one configured
/// <see cref="BoduConfigurationKeyOptions" /> instance between parse and <see cref="BoduConfigurationResolveOptions" />
/// keeps the parsed model and the resolved view's lookups consistent. Instances are safe to cache and share across
/// threads because every property is <c>init</c>-only.
/// </para>
/// </remarks>
public sealed partial class BoduConfigurationParseOptions
{
    /// <summary>
    /// Gets the behaviour profile this option bag represents. The default is
    /// <see cref="BoduConfigurationProfile.Bodu" />.
    /// </summary>
    /// <returns>The selected profile.</returns>
    public BoduConfigurationProfile Profile { get; init; } = BoduConfigurationProfile.Bodu;

    /// <summary>
    /// Gets the inline comment handling mode used by the reader.
    /// </summary>
    /// <returns>The selected inline comment mode.</returns>
    public BoduConfigurationInlineCommentMode InlineCommentMode { get; init; } =
        BoduConfigurationInlineCommentMode.WhitespaceIntroduced;

    /// <summary>
    /// Gets the duplicate key handling mode used by the reader.
    /// </summary>
    /// <returns>The selected duplicate key mode.</returns>
    public IniDuplicateKeyBehavior DuplicateKeyMode { get; init; } =
        IniDuplicateKeyBehavior.LastWins;

    /// <summary>
    /// Gets the duplicate section handling mode used by the reader.
    /// </summary>
    /// <returns>The selected duplicate section mode.</returns>
    public IniDuplicateSectionBehavior DuplicateSectionMode { get; init; } =
        IniDuplicateSectionBehavior.Preserve;

    /// <summary>
    /// Gets the diagnostic routing mode that controls whether recoverable errors throw, are collected on the document,
    /// or are silently ignored.
    /// </summary>
    /// <returns>The selected diagnostic mode.</returns>
    public BoduConfigurationDiagnosticMode DiagnosticMode { get; init; } =
        BoduConfigurationDiagnosticMode.Throw;

    /// <summary>
    /// Gets the maximum permitted length of an individual line, in characters.
    /// </summary>
    /// <returns>A positive line-length cap.</returns>
    public int MaxLineLength { get; init; } = 8192;

    /// <summary>
    /// Gets the maximum permitted length of an individual key, in characters.
    /// </summary>
    /// <returns>A positive key-length cap.</returns>
    public int MaxKeyLength { get; init; } = 1024;

    /// <summary>
    /// Gets the key options used to split raw keys into segments and map them to the configuration key shape.
    /// </summary>
    /// <returns>The selected key options.</returns>
    public BoduConfigurationKeyOptions KeyOptions { get; init; } = BoduConfigurationKeyOptions.Default;

    /// <summary>
    /// Gets a value indicating whether keys and values should be trimmed of leading and trailing whitespace.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the parser trims; otherwise, <see langword="false" />. The default is
    /// <see langword="true" />, matching EditorConfig.
    /// </returns>
    public bool TrimKeysAndValues { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether key-only properties (lines with no <c>=</c>) are permitted.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when key-only lines are permitted; otherwise, <see langword="false" />. The default is
    /// <see langword="false" />.
    /// </returns>
    public bool AllowKeyOnlyProperties { get; init; }

    /// <summary>
    /// Gets the encoding to assume when loading a configuration document from a byte stream without a byte order mark.
    /// The default is <see cref="Encoding.UTF8" />.
    /// </summary>
    /// <returns>The default encoding.</returns>
    public Encoding DefaultEncoding { get; init; } = Encoding.UTF8;

    /// <summary>
    /// Returns the subset of these options that maps onto an <see cref="Bodu.Text.Formats.IniParseOptions" />. Useful
    /// when callers want to delegate basic INI parsing to <see cref="Bodu.Text.Formats.Ini" /> and layer
    /// Configuration-specific features (globs, resolution, trivia) on top.
    /// </summary>
    /// <returns>
    /// A projection that preserves duplicate-key handling and case sensitivity. Configuration features without an INI
    /// equivalent — inline comments, diagnostics, preamble — are not exposed by the projection.
    /// </returns>
    public IniParseOptions ToIniParseOptions() =>
        new()
        {
            AllowGlobalSection = true,
            CaseSensitiveKeys = KeyOptions.CaseSensitive,
            CaseSensitiveSections = KeyOptions.CaseSensitive,
            DuplicateKeyBehavior = DuplicateKeyMode,
            DuplicateSectionBehavior = DuplicateSectionMode,
            PreserveComments = true,
        };
}
