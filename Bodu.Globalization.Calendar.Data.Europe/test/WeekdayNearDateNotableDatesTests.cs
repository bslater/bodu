// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayNearDateNotableDatesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies that the <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy resolves end to end through the
/// Europe data pack — the Finnish and Swedish Saturday-window holidays (Midsummer Day, All Saints' Day) and the German
/// Repentance Day (the Wednesday before 23 November in Saxony) — to the correct civil dates.
/// </summary>
[TestClass]
public sealed class WeekdayNearDateNotableDatesTests
{
    /// <summary>
    /// Verifies that each weekday-near-date holiday resolves to its expected date and weekday for the given year.
    /// </summary>
    /// <param name="expectation">The known-answer row supplied by <see cref="WeekdayNearDateAnswers.Occurrences" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(WeekdayNearDateAnswers.Occurrences),
        typeof(WeekdayNearDateAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void GetNotableDates_WhenWeekdayNearDateRule_ShouldResolveExpectedDate(TerritoryNotableDateKnownAnswer expectation) =>
        TerritoryNotableDateAssertions.AssertExpectedOccurrence(expectation);
}
