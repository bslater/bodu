// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjacentHolidayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies conflict-avoiding substitution: when a holiday's substitute day is already claimed by another non-working
/// holiday (its actual or its observed date), the substitute advances to the next free working day. Christmas and
/// Boxing Day are the canonical pair.
/// </summary>
[TestClass]
public sealed class AdjacentHolidayTests
{
    /// <summary>
    /// Builds a service over the adjacent-holidays fixture (Christmas and Boxing Day, each weekend-substituted with
    /// <c>skipNonWorkingDates</c>).
    /// </summary>
    /// <returns>A service for the adjacent-holidays fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("adjacent-holidays.xml");

    /// <summary>
    /// Verifies that when Christmas falls on a Saturday (observed Monday) and Boxing Day falls on the Sunday, Boxing
    /// Day's substitute advances past the claimed Monday to Tuesday. (2021)
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Resolve_WhenChristmasSaturdayAndBoxingSunday_AdvancesBoxingToTuesday()
    {
        IReadOnlyList<NotableDate> results = CreateService()
            .Resolve(new DateRange(new DateOnly(2021, 12, 20), new DateOnly(2021, 12, 31)), "AU");

        NotableDate christmas = Single(results, "christmas-day");
        Assert.AreEqual(
            (new DateOnly(2021, 12, 27), (DateOnly?)new DateOnly(2021, 12, 25), true),
            (christmas.Date, christmas.ActualDate, christmas.IsObserved));

        NotableDate boxing = Single(results, "boxing-day");
        Assert.AreEqual(
            (new DateOnly(2021, 12, 28), (DateOnly?)new DateOnly(2021, 12, 26), true),
            (boxing.Date, boxing.ActualDate, boxing.IsObserved));
    }

    /// <summary>
    /// Verifies that when Christmas falls on a Sunday and Boxing Day on the Monday, Christmas's substitute skips Boxing
    /// Day's actual Monday and lands on Tuesday, while Boxing Day keeps its Monday. (2016)
    /// </summary>
    [TestMethod]
    public void Resolve_WhenChristmasSundayAndBoxingMonday_AdvancesChristmasPastBoxingActual()
    {
        IReadOnlyList<NotableDate> results = CreateService()
            .Resolve(new DateRange(new DateOnly(2016, 12, 20), new DateOnly(2016, 12, 31)), "AU");

        NotableDate christmas = Single(results, "christmas-day");
        Assert.AreEqual(
            (new DateOnly(2016, 12, 27), (DateOnly?)new DateOnly(2016, 12, 25), true),
            (christmas.Date, christmas.ActualDate, christmas.IsObserved));

        NotableDate boxing = Single(results, "boxing-day");
        Assert.AreEqual(
            (new DateOnly(2016, 12, 26), false),
            (boxing.Date, boxing.IsObserved));
    }

    /// <summary>
    /// Verifies that a single-day query agrees with the range result, emitting the expected observed concept on its
    /// substitute day. In 2021 Christmas is observed on Monday 27 December and Boxing Day on Tuesday 28 December.
    /// </summary>
    /// <param name="month">The queried month.</param>
    /// <param name="day">The queried day of December 2021.</param>
    /// <param name="expectedId">The concept id expected on that day.</param>
    [TestMethod]
    [DataRow(12, 27, "christmas-day")]  // Christmas observed Monday
    [DataRow(12, 28, "boxing-day")]     // Boxing Day observed Tuesday
    public void Resolve_WhenQueriedByDay_EmitsExpectedConcept(int month, int day, string expectedId)
    {
        NotableDate match = Single(CreateService().Resolve(new DateOnly(2021, month, day), "AU"), expectedId);

        Assert.AreEqual(expectedId, match.NotableDateId);
    }

    /// <summary>
    /// Verifies that a weekend actual date suppressed by an observed-only substitution emits nothing. In 2021 the Saturday
    /// 25 December Christmas actual and the Sunday 26 December Boxing Day actual are both suppressed.
    /// </summary>
    /// <param name="month">The queried month.</param>
    /// <param name="day">The queried day of December 2021.</param>
    [TestMethod]
    [DataRow(12, 25)]  // Christmas Saturday actual suppressed
    [DataRow(12, 26)]  // Boxing Day Sunday actual suppressed
    public void Resolve_WhenQueriedOnSuppressedWeekendActual_EmitsNothing(int month, int day)
    {
        Assert.IsEmpty(CreateService().Resolve(new DateOnly(2021, month, day), "AU"));
    }

    /// <summary>
    /// Returns the single resolved occurrence with the supplied notable-date id, asserting exactly one match.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(IReadOnlyList<NotableDate> results, string notableDateId)
    {
        var matches = results.Where(r => r.NotableDateId == notableDateId).ToList();
        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}'");
        return matches[0];
    }
}
