// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarAlgorithmKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a notable-date algorithm: the year under test, the expected resulting date,
/// and optional calendar / territory qualifiers consumed by algorithms whose output depends on cultural context (Easter
/// / Orthodox Easter / Lunar New Year / Vesak / etc.).
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Year">The Gregorian-equivalent year supplied to the algorithm.</param>
/// <param name="ExpectedDate">The expected date the algorithm must return.</param>
/// <param name="Calendar">
/// An optional calendar identifier consumed by calendar-aware algorithms; <see langword="null" /> selects the
/// algorithm's default.
/// </param>
/// <param name="Territory">
/// An optional territory code consumed by algorithms whose output differs per jurisdiction; <see langword="null" />
/// selects the algorithm's default.
/// </param>
public sealed record CalendarAlgorithmKat(
    string Name,
    int Year,
    DateOnly ExpectedDate,
    string? Calendar = null,
    string? Territory = null) : IKat;
