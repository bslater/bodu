// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringFormattingKnownAnswer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Tests.KnownAnswers;

/// <summary>
/// Represents a single string formatting known-answer scenario: an input, the expected output, and the
/// configuration the formatter is expected to honour.
/// </summary>
/// <param name="Id">Stable identifier such as <c>TITLE-0017</c>.</param>
/// <param name="Operation">The formatting operation under test.</param>
/// <param name="Scenario">Human-readable description.</param>
/// <param name="Input">The string passed into the formatter.</param>
/// <param name="Expected">The string the formatter is expected to produce.</param>
/// <param name="CultureName">Culture identifier (e.g. <c>"tr-TR"</c>) when culture-aware behaviour matters.</param>
/// <param name="Acronyms">Known-acronym list available to the formatter.</param>
/// <param name="MinorWords">Connective words eligible for title-case down-casing.</param>
/// <param name="PreserveExistingAcronyms">Whether all-uppercase tokens in input should survive casing.</param>
/// <param name="PreserveMixedCaseWords">Whether mixed-case tokens like <c>OAuth</c>, <c>iPhone</c> survive.</param>
/// <param name="PreserveWordsContainingDigits">Whether tokens containing digits survive casing changes.</param>
/// <param name="NormalizeDiacritics">Whether the formatter should strip diacritical marks.</param>
/// <param name="Notes">Optional commentary explaining quirks or expected limitations.</param>
public sealed record StringFormattingKnownAnswer(
    string Id,
    StringFormattingOperation Operation,
    string Scenario,
    string Input,
    string Expected,
    string? CultureName = null,
    IReadOnlyCollection<string>? Acronyms = null,
    IReadOnlyCollection<string>? MinorWords = null,
    bool PreserveExistingAcronyms = true,
    bool PreserveMixedCaseWords = true,
    bool PreserveWordsContainingDigits = true,
    bool NormalizeDiacritics = false,
    string? Notes = null)
{
    /// <summary>
    /// Gets a short display name combining identifier, operation, and scenario for test runner output.
    /// </summary>
    public string DisplayName => $"{Id} {Operation} — {Scenario}";
}
