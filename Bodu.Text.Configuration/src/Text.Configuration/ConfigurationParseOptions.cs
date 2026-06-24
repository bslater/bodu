// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini;

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
/// <see cref="For(ConfigurationProfile)" /> when the profile is data-driven (for example, read from configuration).
/// </para>
/// <para>
/// <see cref="DiagnosticMode" /> is the most consequential knob. In <see cref="ConfigurationDiagnosticMode.Throw" />
/// the reader raises <see cref="ConfigurationParseException" /> on the first recoverable error; in
/// <see cref="ConfigurationDiagnosticMode.Collect" /> the parser drives through the document and reports every
/// recoverable issue via the parse result's <see cref="ConfigurationParseResult.Diagnostics" /> list. Pair the latter
/// with <see cref="ConfigurationDocument.ParseWithDiagnostics(string, ConfigurationParseOptions?)" />.
/// </para>
/// <para>
/// Key shape is delegated to <see cref="KeyOptions" />. Sharing one configured <see cref="ConfigurationKeyOptions" />
/// instance between parse and <see cref="ConfigurationResolveOptions" /> keeps the parsed model and the resolved view's
/// lookups consistent. Instances are safe to cache and share across threads because every property is <c>init</c>-only.
/// </para>
/// </remarks>
public sealed partial class ConfigurationParseOptions
{
    /// <summary>
    /// Gets the behaviour profile this option bag represents. The default is <see cref="ConfigurationProfile.Bodu" />.
    /// </summary>
    /// <value>The selected profile.</value>
    public ConfigurationProfile Profile { get; init; } = ConfigurationProfile.Bodu;

    /// <summary>
    /// Gets the inline comment handling mode used by the reader.
    /// </summary>
    /// <value>The selected inline comment mode.</value>
    public ConfigurationInlineCommentMode InlineCommentMode { get; init; } =
        ConfigurationInlineCommentMode.WhitespaceIntroduced;

    /// <summary>
    /// Gets the duplicate key handling mode used by the reader.
    /// </summary>
    /// <value>The selected duplicate key mode.</value>
    public DuplicateKeyPolicy DuplicateKeyMode { get; init; } =
        DuplicateKeyPolicy.LastWins;

    /// <summary>
    /// Gets the duplicate section handling mode used by the reader.
    /// </summary>
    /// <value>The selected duplicate section mode.</value>
    public IniDuplicateSectionBehavior DuplicateSectionMode { get; init; } =
        IniDuplicateSectionBehavior.Preserve;

    /// <summary>
    /// Gets the section-header trailing-content handling mode used by the reader. The default,
    /// <see cref="ConfigurationSectionHeaderMode.Lenient" />, accepts trailing words silently and matches the
    /// historical Bodu profile; the EditorConfig-compatible and strict presets use
    /// <see cref="ConfigurationSectionHeaderMode.Strict" /> instead.
    /// </summary>
    /// <value>The selected section-header mode.</value>
    public ConfigurationSectionHeaderMode SectionHeaderMode { get; init; } =
        ConfigurationSectionHeaderMode.Lenient;

    /// <summary>
    /// Gets the diagnostic routing mode that controls whether recoverable errors throw, are collected on the document,
    /// or are silently ignored.
    /// </summary>
    /// <value>The selected diagnostic mode.</value>
    public ConfigurationDiagnosticMode DiagnosticMode { get; init; } =
        ConfigurationDiagnosticMode.Throw;

    /// <summary>
    /// Gets the maximum permitted length of an individual line, in characters.
    /// </summary>
    /// <value>A positive line-length cap.</value>
    public int MaxLineLength { get; init; } = 8192;

    /// <summary>
    /// Gets the maximum permitted length of an individual key, in characters.
    /// </summary>
    /// <value>A positive key-length cap.</value>
    public int MaxKeyLength { get; init; } = 1024;

    /// <summary>
    /// Gets the key options used to split raw keys into segments and map them to the configuration key shape.
    /// </summary>
    /// <value>The selected key options.</value>
    public ConfigurationKeyOptions KeyOptions { get; init; } = ConfigurationKeyOptions.Default;

    /// <summary>
    /// Gets a value indicating whether keys and values should be trimmed of leading and trailing whitespace.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the parser trims; otherwise, <see langword="false" />. The default is
    /// <see langword="true" />, matching EditorConfig.
    /// </value>
    public bool TrimKeysAndValues { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether key-only properties (lines with no <c>=</c>) are permitted.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when key-only lines are permitted; otherwise, <see langword="false" />. The default is
    /// <see langword="false" />.
    /// </value>
    public bool AllowKeyOnlyProperties { get; init; }

    /// <summary>
    /// Gets the encoding to assume when loading a configuration document from a byte stream without a byte order mark.
    /// The default is <see cref="Encoding.UTF8" />.
    /// </summary>
    /// <value>The default encoding.</value>
    public Encoding DefaultEncoding { get; init; } = Encoding.UTF8;

    /// <summary>
    /// Returns the subset of these options that maps onto an <see cref="Bodu.Text.Ini.IniParseOptions" />. Useful when
    /// callers want to delegate basic INI parsing to <see cref="Bodu.Text.Ini.Ini" /> and layer Configuration-specific
    /// features (globs, resolution, trivia) on top.
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
