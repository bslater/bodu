// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjacentHolidayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

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
        Assert.AreEqual(new DateOnly(2021, 12, 27), christmas.Date, "Christmas observed Monday");
        Assert.AreEqual(new DateOnly(2021, 12, 25), christmas.ActualDate);
        Assert.IsTrue(christmas.IsObserved);

        NotableDate boxing = Single(results, "boxing-day");
        Assert.AreEqual(new DateOnly(2021, 12, 28), boxing.Date, "Boxing Day advances to Tuesday");
        Assert.AreEqual(new DateOnly(2021, 12, 26), boxing.ActualDate);
        Assert.IsTrue(boxing.IsObserved);
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
        Assert.AreEqual(new DateOnly(2016, 12, 27), christmas.Date, "Christmas substitute skips Boxing Day's Monday");
        Assert.AreEqual(new DateOnly(2016, 12, 25), christmas.ActualDate);
        Assert.IsTrue(christmas.IsObserved);

        NotableDate boxing = Single(results, "boxing-day");
        Assert.AreEqual(new DateOnly(2016, 12, 26), boxing.Date, "Boxing Day keeps its Monday");
        Assert.IsFalse(boxing.IsObserved);
    }

    /// <summary>
    /// Verifies that single-day queries agree with the range result and that the suppressed weekend actuals are not
    /// emitted. (2021)
    /// </summary>
    [TestMethod]
    public void Resolve_WhenQueriedByDay_IsConsistentWithRangeAndSuppressesActuals()
    {
        NotableDateService service = CreateService();

        Assert.AreEqual("christmas-day", Single(service.Resolve(new DateOnly(2021, 12, 27), "AU"), "christmas-day").NotableDateId);
        Assert.AreEqual("boxing-day", Single(service.Resolve(new DateOnly(2021, 12, 28), "AU"), "boxing-day").NotableDateId);

        Assert.AreEqual(0, service.Resolve(new DateOnly(2021, 12, 25), "AU").Count, "Christmas Saturday actual suppressed");
        Assert.AreEqual(0, service.Resolve(new DateOnly(2021, 12, 26), "AU").Count, "Boxing Day Sunday actual suppressed");
    }

    /// <summary>
    /// Returns the single resolved occurrence with the supplied notable-date id, asserting exactly one match.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(IReadOnlyList<NotableDate> results, string notableDateId)
    {
        List<NotableDate> matches = results.Where(r => r.NotableDateId == notableDateId).ToList();
        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}'");
        return matches[0];
    }
}
