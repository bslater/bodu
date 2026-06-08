// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Controls how the INI parser interprets source text, including dialect settings, duplicate handling, and
/// case-sensitivity for section names and keys.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IniParseOptions" /> is an immutable value type. Construct one using object initializer syntax with
/// <see langword="init" /> properties:
/// </para>
/// <example>
/// <code>
///<![CDATA[
/// IniParseOptions options = new IniParseOptions
/// {
///     AllowGlobalSection = false,
///     DuplicateKeyBehavior = DuplicateKeyPolicy.FirstWins,
/// };
///]]>
/// </code>
/// </example>
/// <para>
/// The <see cref="Default" /> field exposes a pre-constructed instance with all properties set to their default values,
/// which is suitable for most INI files.
/// </para>
/// </remarks>
public readonly struct IniParseOptions
{
    /// <summary>
    /// Gets a pre-constructed <see cref="IniParseOptions" /> instance with all properties set to their default values.
    /// </summary>
    /// <returns>An <see cref="IniParseOptions" /> equivalent to <c>new IniParseOptions()</c>.</returns>
    public static readonly IniParseOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IniParseOptions" /> struct with all properties set to their default
    /// values.
    /// </summary>
    public IniParseOptions()
    {
    }

    /// <summary>
    /// Gets a value indicating whether keys that appear before the first section header are accepted and collected into
    /// the global section.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> (the default) to collect pre-section keys into <see cref="IniDocumentBase.GlobalSection" />;
    /// <see langword="false" /> to throw <see cref="IniFormatException" /> on the first such key.
    /// </returns>
    public bool AllowGlobalSection { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether section-name comparisons are case-sensitive.
    /// </summary>
    /// <returns>
    /// <see langword="false" /> (the default) for case-insensitive section lookup; <see langword="true" /> for ordinal
    /// case-sensitive lookup.
    /// </returns>
    public bool CaseSensitiveSections { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether key comparisons within a section are case-sensitive.
    /// </summary>
    /// <returns>
    /// <see langword="false" /> (the default) for case-insensitive key lookup; <see langword="true" /> for ordinal
    /// case-sensitive lookup.
    /// </returns>
    public bool CaseSensitiveKeys { get; init; } = false;

    /// <summary>
    /// Gets the behavior applied when the same key appears more than once within a section.
    /// </summary>
    /// <returns><see cref="DuplicateKeyPolicy.LastWins" /> by default.</returns>
    public DuplicateKeyPolicy DuplicateKeyBehavior { get; init; } = DuplicateKeyPolicy.LastWins;

    /// <summary>
    /// Gets the behavior applied when the same section name appears more than once in the source.
    /// </summary>
    /// <returns><see cref="IniDuplicateSectionBehavior.Merge" /> by default.</returns>
    public IniDuplicateSectionBehavior DuplicateSectionBehavior { get; init; } = IniDuplicateSectionBehavior.Merge;

    /// <summary>
    /// Gets a value indicating whether the parser captures full-line comments as trivia and attaches them to the next
    /// entry or section as <see cref="IniEntry.LeadingComments" /> and <see cref="IniSection.LeadingComments" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> (the default) to retain comments for round-trip and tooling scenarios;
    /// <see langword="false" /> to discard comments and behave like a classic INI parser.
    /// </returns>
    public bool PreserveComments { get; init; } = true;
}
