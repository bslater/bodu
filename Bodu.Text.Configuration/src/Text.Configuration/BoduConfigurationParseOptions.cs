// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how a configuration document is parsed: comment handling, duplicate handling, diagnostic routing,
/// length limits, and the key mapping options.
/// </summary>
/// <remarks>
/// Use <see cref="For(BoduConfigurationProfile)" /> to obtain an options bag that matches one of the predefined
/// behaviour profiles. The static presets (<see cref="Bodu" />, <see cref="EditorConfigCompatible" />,
/// <see cref="Strict" />, <see cref="Relaxed" />) are thin getters that call into <see cref="For" />.
/// </remarks>
public sealed partial class BoduConfigurationParseOptions
{
    /// <summary>
    /// Gets the behaviour profile this option bag represents. The default is <see cref="BoduConfigurationProfile.Bodu" />.
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
    public BoduConfigurationDuplicateKeyMode DuplicateKeyMode { get; init; } =
        BoduConfigurationDuplicateKeyMode.LastWins;

    /// <summary>
    /// Gets the duplicate section handling mode used by the reader.
    /// </summary>
    /// <returns>The selected duplicate section mode.</returns>
    public BoduConfigurationDuplicateSectionMode DuplicateSectionMode { get; init; } =
        BoduConfigurationDuplicateSectionMode.Preserve;

    /// <summary>
    /// Gets the diagnostic routing mode that controls whether recoverable errors throw, are collected on the
    /// document, or are silently ignored.
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
    /// <returns><see langword="true" /> when the parser trims; otherwise, <see langword="false" />. The default
    /// is <see langword="true" />, matching EditorConfig.</returns>
    public bool TrimKeysAndValues { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether key-only properties (lines with no <c>=</c>) are permitted.
    /// </summary>
    /// <returns><see langword="true" /> when key-only lines are permitted; otherwise, <see langword="false" />.
    /// The default is <see langword="false" />.</returns>
    public bool AllowKeyOnlyProperties { get; init; }

    /// <summary>
    /// Gets the encoding to assume when loading a configuration document from a byte stream without a byte
    /// order mark. The default is <see cref="Encoding.UTF8" />.
    /// </summary>
    /// <returns>The default encoding.</returns>
    public Encoding DefaultEncoding { get; init; } = Encoding.UTF8;
}
