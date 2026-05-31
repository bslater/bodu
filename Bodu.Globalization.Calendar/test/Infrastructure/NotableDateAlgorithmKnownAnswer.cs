// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmKnownAnswer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Represents a single known-answer test (KAT) row for an <see cref="INotableDateAlgorithm" />: the algorithm
/// label, a factory that constructs a fresh instance, the input <see cref="Year" /> and
/// <see cref="CalendarKind" />, and the expected <see cref="DateTime" /> result with an optional
/// <see cref="ToleranceDays" /> window.
/// </summary>
/// <remarks>
/// <para>
/// Rows are consumed by the consolidated <c>GetDate_WhenGivenSmokeRows_ShouldReturnExpectedDateWithinTolerance</c>
/// and <c>GetDate_WhenGivenAllKnownRows_ShouldReturnExpectedDateWithinTolerance</c> tests in each
/// <c>*NotableDateAlgorithmTests.cs</c> file. The same record covers exact-match algorithms (Easter, Vesak,
/// Asalha, Qingming) with <see cref="ToleranceDays" /> equal to <c>0</c>, and approximation algorithms (Losar,
/// Hindu lunar) by setting a small day-count tolerance.
/// </para>
/// <para>
/// Provider methods in <see cref="NotableDateAlgorithmKnownAnswers" /> wrap each row in
/// <c>new object[] { row }</c> so MSTest's <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" />
/// can hand the row directly to the test method as its single parameter. The display name produced by
/// <see cref="NotableDateAlgorithmKnownAnswers.GetDisplayName" /> formats the row as
/// <c>"Algorithm | Year | CalendarKind -&gt; ExpectedDate [+/- Nd] [Source]"</c>.
/// </para>
/// </remarks>
public sealed record NotableDateAlgorithmKnownAnswer : IKat
{
    /// <inheritdoc />
    /// <remarks>Synthesizes a compact <c>"Algorithm Year CalendarKind"</c> label so rows remain distinguishable when surfaced via <see cref="KatDisplayName" />.</remarks>
    string IKat.Name => $"{Algorithm} {Year} {CalendarKind}";

    /// <summary>
    /// Gets the short algorithm label, for example <c>"Easter"</c>, <c>"Losar"</c>, or
    /// <c>"HinduLunar(Diwali)"</c>. Surfaces in the MSTest test explorer via
    /// <see cref="NotableDateAlgorithmKnownAnswers.GetDisplayName" />.
    /// </summary>
    /// <returns>The algorithm label rendered in row display names.</returns>
    public required string Algorithm { get; init; }

    /// <summary>
    /// Gets the delegate that constructs a fresh <see cref="INotableDateAlgorithm" /> instance for this row.
    /// </summary>
    /// <returns>A factory invoked once per row so per-instance caches do not leak across assertions.</returns>
    public required Func<INotableDateAlgorithm> Factory { get; init; }

    /// <summary>
    /// Gets the year passed to
    /// <see cref="INotableDateAlgorithm.GetDate(int, System.Globalization.Calendar?)" /> for this row.
    /// </summary>
    /// <returns>The Gregorian-equivalent year under test.</returns>
    public required int Year { get; init; }

    /// <summary>
    /// Gets the <see cref="DateTime" /> the algorithm is expected to return for the row's
    /// (<see cref="Year" />, <see cref="CalendarKind" />) pair.
    /// </summary>
    /// <returns>The expected result. When <see cref="ToleranceDays" /> is greater than zero the assertion
    /// admits any value within +/- <see cref="ToleranceDays" /> of this date.</returns>
    public required DateTime ExpectedDate { get; init; }

    /// <summary>
    /// Gets the calendar system the test should pass through to the algorithm. <see cref="AlgorithmCalendarKind.Default" />
    /// supplies <see langword="null" /> (the algorithm's default), <see cref="AlgorithmCalendarKind.Gregorian" />
    /// supplies a <see cref="System.Globalization.GregorianCalendar" />, and
    /// <see cref="AlgorithmCalendarKind.Julian" /> supplies a <see cref="System.Globalization.JulianCalendar" />.
    /// </summary>
    /// <returns>The calendar encoding for the row.</returns>
    public AlgorithmCalendarKind CalendarKind { get; init; } = AlgorithmCalendarKind.Default;

    /// <summary>
    /// Gets the symmetric tolerance window applied to the assertion. A value of <c>0</c> (the default)
    /// requires an exact match against <see cref="ExpectedDate" />; positive values allow the result to fall
    /// within +/- <see cref="ToleranceDays" /> calendar days of the expected date.
    /// </summary>
    /// <remarks>
    /// Used by approximation algorithms — for example Losar (lunar approximation, <c>1</c>), Hindu lunar
    /// festivals (<c>2</c>), Qingming (<c>1</c>) — where the published almanac date may drift by a small
    /// number of days from the algorithm's mathematical result.
    /// </remarks>
    /// <returns>The non-negative tolerance window, in days.</returns>
    public int ToleranceDays { get; init; } = 0;

    /// <summary>
    /// Gets an optional provenance label appended to the row's display name, for example
    /// <c>"Astronomical Society of the Pacific 2024"</c> or <c>"TMAI Tibetan calendar"</c>.
    /// </summary>
    /// <returns>The source citation, or <see langword="null" /> when no provenance is recorded.</returns>
    public string? Source { get; init; }
}
