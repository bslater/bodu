// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarDataKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row that validates a calendar data package through the public calendar
/// service: a (territory, calendar, year, notable-date) query and the expected actual / observed dates
/// the bundled rule provider must surface.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Territory">The territory code passed to the service.</param>
/// <param name="Calendar">The calendar identifier.</param>
/// <param name="Year">The query year.</param>
/// <param name="NotableDateName">The canonical name of the notable date under test, for example <c>"Anzac Day"</c>.</param>
/// <param name="ExpectedDate">The expected actual date.</param>
/// <param name="ExpectedObservedDate">The expected observed date when a weekend-substitute or other adjustment applies; <see langword="null" /> when no adjustment is expected.</param>
public sealed record CalendarDataKat(
    string Name,
    string Territory,
    string Calendar,
    int Year,
    string NotableDateName,
    DateOnly ExpectedDate,
    DateOnly? ExpectedObservedDate = null) : IKat;
