// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniKnownAnswerVector.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a positive Known Answer Test (KAT) vector for the INI file format. A vector pairs a raw INI source
/// string with the expected parsed state — global entries and named sections — so that data-driven tests can
/// verify <see cref="Ini" /> against authoritative references and common-practice inputs.
/// </summary>
/// <param name="Description">A short human-readable label identifying the vector.</param>
/// <param name="Input">The raw INI source text to parse.</param>
/// <param name="GlobalEntries">
/// The expected key/value entries in the global section, in source order. Pass an empty array when no global
/// entries are expected.
/// </param>
/// <param name="Sections">
/// The expected named sections in source order. Each element carries the section name and its key/value entries.
/// Pass an empty array when no named sections are expected.
/// </param>
/// <param name="Options">
/// The parse options to apply. <see langword="null" /> indicates that <see cref="IniParseOptions.Default" />
/// should be used.
/// </param>
/// <param name="Source">The citation for the vector's origin (specification clause, reference implementation, etc.).</param>
public sealed record IniKnownAnswerVector(
    string Description,
    string Input,
    (string Key, string Value)[] GlobalEntries,
    (string Name, (string Key, string Value)[] Entries)[] Sections,
    IniParseOptions? Options = null,
    string? Source = null)
{
    /// <summary>
    /// Returns the human-readable label, used by MSTest's <c>[DynamicData]</c> infrastructure when generating
    /// test names so each row is identifiable in failure output.
    /// </summary>
    /// <returns>The description, optionally suffixed with the citation in square brackets.</returns>
    public override string ToString() =>
        Source is null ? Description : $"{Description} [{Source}]";
}
