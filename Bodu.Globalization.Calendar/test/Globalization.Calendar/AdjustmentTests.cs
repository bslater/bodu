// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the adjustment matrix end to end: every emission mode, day-shifting action, and trigger type, including
/// cross-year observed shifts. The fixture anchors each example on 1 January, whose weekday varies by year (2022 is a
/// Saturday, 2023 a Sunday, 2026 a Thursday).
/// </summary>
[TestClass]
public sealed class AdjustmentTests
{
    private const string Territory = "ZZ";

    /// <summary>
    /// Builds a resolver over the adjustments fixture.
    /// </summary>
    /// <returns>A resolver for the adjustments fixture.</returns>
    private static NotableDateService CreateResolver() =>
        NotableDateFixtures.Resolver("adjustments.xml");

    /// <summary>
    /// Verifies that the observed-only emission suppresses the weekend actual date, emitting nothing on 1 January 2022 (a
    /// Saturday).
    /// </summary>
    [TestMethod]
    public void Emission_ObservedOnly_WhenWeekendActualDate_ShouldSuppressActual()
    {
        Assert.AreEqual(0, Count(CreateResolver().Resolve(new DateOnly(2022, 1, 1), Territory), "observed-only"));
    }

    /// <summary>
    /// Verifies that the observed-only emission emits the observed Monday occurrence carrying the actual date and the
    /// adjustment-policy id. 1 January 2022 (a Saturday) is observed on Monday 3 January.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Emission_ObservedOnly_WhenObservedMonday_ShouldEmitObservedWithPolicy()
    {
        NotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2022, 1, 3), Territory), "observed-only");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2022, 1, 1), (string?)"weekend-next-monday-observed"),
            (observed.IsObserved, observed.ActualDate, observed.AdjustmentPolicyId));
    }

    /// <summary>
    /// Verifies that the actual-and-observed emission emits both the actual weekend date and the observed Monday.
    /// </summary>
    [TestMethod]
    public void Emission_ActualAndObserved_EmitsBothDates()
    {
        IReadOnlyList<NotableDate> results = CreateResolver()
            .Resolve(new DateRange(new DateOnly(2022, 1, 1), new DateOnly(2022, 1, 3)), Territory)
            .Where(r => r.NotableDateId == "actual-and-observed")
            .ToList();

        CollectionAssert.AreEqual(
            new[] { (new DateOnly(2022, 1, 1), false), (new DateOnly(2022, 1, 3), true) },
            results.OrderBy(r => r.Date).Select(r => (r.Date, r.IsObserved)).ToArray());
    }

    /// <summary>
    /// Verifies that the observed-as-additional emission emits the actual date plus an additional observed occurrence.
    /// </summary>
    [TestMethod]
    public void Emission_ObservedAsAdditional_EmitsActualPlusObserved()
    {
        IReadOnlyList<NotableDate> results = CreateResolver()
            .Resolve(new DateRange(new DateOnly(2022, 1, 1), new DateOnly(2022, 1, 3)), Territory)
            .Where(r => r.NotableDateId == "observed-additional")
            .ToList();

        CollectionAssert.AreEqual(
            new[] { (new DateOnly(2022, 1, 1), false), (new DateOnly(2022, 1, 3), true) },
            results.OrderBy(r => r.Date).Select(r => (r.Date, r.IsObserved)).ToArray());
    }

    /// <summary>
    /// Verifies that the actual-only emission keeps the weekend actual date as an unobserved occurrence carrying no
    /// adjustment-policy id.
    /// </summary>
    [TestMethod]
    public void Emission_ActualOnly_WhenWeekendActualDate_ShouldKeepUnobservedActual()
    {
        NotableDate actual = Single(CreateResolver().Resolve(new DateOnly(2022, 1, 1), Territory), "actual-only");

        Assert.AreEqual(
            (false, (string?)null),
            (actual.IsObserved, actual.AdjustmentPolicyId));
    }

    /// <summary>
    /// Verifies that the actual-only emission emits nothing on the computed Monday substitute.
    /// </summary>
    [TestMethod]
    public void Emission_ActualOnly_WhenComputedMonday_ShouldNotEmitObserved()
    {
        Assert.AreEqual(0, Count(CreateResolver().Resolve(new DateOnly(2022, 1, 3), Territory), "actual-only"));
    }

    /// <summary>
    /// Verifies that the suppress emission removes the occurrence when 1 January falls on a weekend (2022, a Saturday).
    /// </summary>
    [TestMethod]
    public void Emission_Suppress_WhenWeekend_ShouldRemoveOccurrence()
    {
        Assert.AreEqual(0, Count(CreateResolver().Resolve(new DateOnly(2022, 1, 1), Territory), "suppress-weekend"));
    }

    /// <summary>
    /// Verifies that the suppress emission leaves the occurrence in place when 1 January falls on a weekday (2026, a
    /// Thursday), emitting the unobserved actual date.
    /// </summary>
    [TestMethod]
    public void Emission_Suppress_WhenWeekday_ShouldKeepOccurrence()
    {
        NotableDate weekday = Single(CreateResolver().Resolve(new DateOnly(2026, 1, 1), Territory), "suppress-weekend");

        Assert.AreEqual(
            (false, new DateOnly(2026, 1, 1)),
            (weekday.IsObserved, weekday.Date));
    }

    /// <summary>
    /// Verifies that an always trigger with an add-days action shifts the occurrence forward by the configured day,
    /// emitting an observed 2 January 2026 that carries the 1 January actual date.
    /// </summary>
    [TestMethod]
    public void Action_AddDays_WithAlwaysTrigger_WhenShifted_ShouldEmitObservedWithActualDate()
    {
        NotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2026, 1, 2), Territory), "always-shift");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 1)),
            (observed.IsObserved, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the add-days shift, emitted observed-only, suppresses the unshifted 1 January actual date.
    /// </summary>
    [TestMethod]
    public void Action_AddDays_WithAlwaysTrigger_WhenActualDate_ShouldSuppressActual()
    {
        Assert.AreEqual(0, Count(CreateResolver().Resolve(new DateOnly(2026, 1, 1), Territory), "always-shift"));
    }

    /// <summary>
    /// Verifies that the move-to-previous-weekday action moves a Sunday occurrence back to the preceding Friday, across
    /// a year boundary.
    /// </summary>
    [TestMethod]
    public void Action_MoveToPreviousWeekday_MovesSundayBackToFriday()
    {
        NotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2022, 12, 30), Territory), "prev-friday");

        Assert.AreEqual(
            (true, new DateOnly(2022, 12, 30), (DateOnly?)new DateOnly(2023, 1, 1)),
            (observed.IsObserved, observed.Date, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the move-to-next-working-day action moves a weekend 1 January occurrence forward to the following
    /// Monday. 1 January 2022 (a Saturday) is observed on Monday 3 January; 1 January 2023 (a Sunday) on Monday 2 January.
    /// </summary>
    /// <param name="year">The observed Monday year.</param>
    /// <param name="month">The observed Monday month.</param>
    /// <param name="day">The observed Monday day.</param>
    [TestMethod]
    [DataRow(2022, 1, 3)]  // 1 Jan 2022 Saturday -> Monday 3 Jan
    [DataRow(2023, 1, 2)]  // 1 Jan 2023 Sunday -> Monday 2 Jan
    public void Action_MoveToNextWorkingDay_WhenWeekendOccurrence_ShouldMoveToMonday(int year, int month, int day)
    {
        DateOnly monday = new(year, month, day);

        Assert.AreEqual(monday, Single(CreateResolver().Resolve(monday, Territory), "next-working-day").Date);
    }

    /// <summary>
    /// Verifies that the move-to-previous-working-day action moves a Saturday occurrence back to the preceding Friday.
    /// </summary>
    [TestMethod]
    public void Action_MoveToPreviousWorkingDay_MovesSaturdayBackToFriday()
    {
        NotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2021, 12, 31), Territory), "prev-working-day");

        Assert.AreEqual(
            (new DateOnly(2021, 12, 31), (DateOnly?)new DateOnly(2022, 1, 1)),
            (observed.Date, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the if-weekday trigger fires on a weekday occurrence, emitting an observed 3 January 2026 that carries
    /// the 1 January actual date.
    /// </summary>
    [TestMethod]
    public void Trigger_IfWeekday_WhenWeekday_ShouldFireAndShift()
    {
        NotableDate weekday = Single(CreateResolver().Resolve(new DateOnly(2026, 1, 3), Territory), "weekday-shift");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 1)),
            (weekday.IsObserved, weekday.ActualDate));
    }

    /// <summary>
    /// Verifies that the if-weekday trigger does not fire on a weekend occurrence, leaving 1 January 2022 (a Saturday)
    /// unobserved.
    /// </summary>
    [TestMethod]
    public void Trigger_IfWeekday_WhenWeekend_ShouldNotFire()
    {
        NotableDate weekend = Single(CreateResolver().Resolve(new DateOnly(2022, 1, 1), Territory), "weekday-shift");

        Assert.IsFalse(weekend.IsObserved);
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

    /// <summary>
    /// Counts the resolved occurrences with the supplied notable-date id.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to count.</param>
    /// <returns>The number of matching occurrences.</returns>
    private static int Count(IReadOnlyList<NotableDate> results, string notableDateId) =>
        results.Count(r => r.NotableDateId == notableDateId);
}
