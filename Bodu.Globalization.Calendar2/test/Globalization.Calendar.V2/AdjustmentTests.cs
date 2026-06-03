// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

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
    private static NotableDateResolver CreateResolver() =>
        CookbookFixtures.Resolver("adjustments.xml");

    /// <summary>
    /// Verifies that the observed-only emission emits only the Monday observance and suppresses the weekend actual date.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Emission_ObservedOnly_EmitsObservedAndSuppressesActual()
    {
        NotableDateResolver resolver = CreateResolver();

        Assert.AreEqual(0, Count(resolver.Resolve(new DateOnly(2022, 1, 1), Territory), "observed-only"), "actual suppressed");

        ResolvedNotableDate observed = Single(resolver.Resolve(new DateOnly(2022, 1, 3), Territory), "observed-only");
        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2022, 1, 1), observed.ActualDate);
        Assert.AreEqual("weekend-next-monday-observed", observed.AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that the actual-and-observed emission emits both the actual weekend date and the observed Monday.
    /// </summary>
    [TestMethod]
    public void Emission_ActualAndObserved_EmitsBothDates()
    {
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver()
            .Resolve(new DateOnly(2022, 1, 1), new DateOnly(2022, 1, 3), Territory)
            .Where(r => r.NotableDateId == "actual-and-observed")
            .ToList();

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 1) && !r.IsObserved), "actual occurrence");
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 3) && r.IsObserved), "observed occurrence");
    }

    /// <summary>
    /// Verifies that the observed-as-additional emission emits the actual date plus an additional observed occurrence.
    /// </summary>
    [TestMethod]
    public void Emission_ObservedAsAdditional_EmitsActualPlusObserved()
    {
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver()
            .Resolve(new DateOnly(2022, 1, 1), new DateOnly(2022, 1, 3), Territory)
            .Where(r => r.NotableDateId == "observed-additional")
            .ToList();

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 1) && !r.IsObserved), "actual occurrence");
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 3) && r.IsObserved), "additional observed occurrence");
    }

    /// <summary>
    /// Verifies that the actual-only emission keeps the weekend actual date and emits nothing on the computed Monday.
    /// </summary>
    [TestMethod]
    public void Emission_ActualOnly_KeepsActualAndIgnoresObserved()
    {
        NotableDateResolver resolver = CreateResolver();

        ResolvedNotableDate actual = Single(resolver.Resolve(new DateOnly(2022, 1, 1), Territory), "actual-only");
        Assert.IsFalse(actual.IsObserved);
        Assert.IsNull(actual.AdjustmentPolicyId);

        Assert.AreEqual(0, Count(resolver.Resolve(new DateOnly(2022, 1, 3), Territory), "actual-only"), "no observed emitted");
    }

    /// <summary>
    /// Verifies that the suppress emission removes the occurrence on a weekend but leaves it on a weekday.
    /// </summary>
    [TestMethod]
    public void Emission_Suppress_RemovesWeekendOccurrenceOnly()
    {
        NotableDateResolver resolver = CreateResolver();

        Assert.AreEqual(0, Count(resolver.Resolve(new DateOnly(2022, 1, 1), Territory), "suppress-weekend"), "weekend suppressed");

        ResolvedNotableDate weekday = Single(resolver.Resolve(new DateOnly(2026, 1, 1), Territory), "suppress-weekend");
        Assert.IsFalse(weekday.IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 1), weekday.Date);
    }

    /// <summary>
    /// Verifies that an always trigger with an add-days action shifts every occurrence by the configured number of days.
    /// </summary>
    [TestMethod]
    public void Action_AddDays_WithAlwaysTrigger_ShiftsEveryOccurrence()
    {
        NotableDateResolver resolver = CreateResolver();

        ResolvedNotableDate observed = Single(resolver.Resolve(new DateOnly(2026, 1, 2), Territory), "always-shift");
        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 1), observed.ActualDate);

        Assert.AreEqual(0, Count(resolver.Resolve(new DateOnly(2026, 1, 1), Territory), "always-shift"), "actual suppressed by observed-only");
    }

    /// <summary>
    /// Verifies that the move-to-previous-weekday action moves a Sunday occurrence back to the preceding Friday, across
    /// a year boundary.
    /// </summary>
    [TestMethod]
    public void Action_MoveToPreviousWeekday_MovesSundayBackToFriday()
    {
        ResolvedNotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2022, 12, 30), Territory), "prev-friday");

        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2022, 12, 30), observed.Date);
        Assert.AreEqual(new DateOnly(2023, 1, 1), observed.ActualDate);
    }

    /// <summary>
    /// Verifies that the move-to-next-working-day action moves weekend occurrences forward to Monday.
    /// </summary>
    [TestMethod]
    public void Action_MoveToNextWorkingDay_MovesWeekendForwardToMonday()
    {
        NotableDateResolver resolver = CreateResolver();

        Assert.AreEqual(new DateOnly(2022, 1, 3), Single(resolver.Resolve(new DateOnly(2022, 1, 3), Territory), "next-working-day").Date);
        Assert.AreEqual(new DateOnly(2023, 1, 2), Single(resolver.Resolve(new DateOnly(2023, 1, 2), Territory), "next-working-day").Date);
    }

    /// <summary>
    /// Verifies that the move-to-previous-working-day action moves a Saturday occurrence back to the preceding Friday.
    /// </summary>
    [TestMethod]
    public void Action_MoveToPreviousWorkingDay_MovesSaturdayBackToFriday()
    {
        ResolvedNotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2021, 12, 31), Territory), "prev-working-day");

        Assert.AreEqual(new DateOnly(2021, 12, 31), observed.Date);
        Assert.AreEqual(new DateOnly(2022, 1, 1), observed.ActualDate);
    }

    /// <summary>
    /// Verifies that the if-weekday trigger fires only on weekdays.
    /// </summary>
    [TestMethod]
    public void Trigger_IfWeekday_FiresOnWeekdaysOnly()
    {
        NotableDateResolver resolver = CreateResolver();

        ResolvedNotableDate weekday = Single(resolver.Resolve(new DateOnly(2026, 1, 3), Territory), "weekday-shift");
        Assert.IsTrue(weekday.IsObserved, "weekday occurrence shifted");
        Assert.AreEqual(new DateOnly(2026, 1, 1), weekday.ActualDate);

        ResolvedNotableDate weekend = Single(resolver.Resolve(new DateOnly(2022, 1, 1), Territory), "weekday-shift");
        Assert.IsFalse(weekend.IsObserved, "weekend occurrence unchanged");
    }

    /// <summary>
    /// Returns the single resolved occurrence with the supplied notable-date id, asserting exactly one match.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static ResolvedNotableDate Single(IReadOnlyList<ResolvedNotableDate> results, string notableDateId)
    {
        List<ResolvedNotableDate> matches = results.Where(r => r.NotableDateId == notableDateId).ToList();
        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}'");
        return matches[0];
    }

    /// <summary>
    /// Counts the resolved occurrences with the supplied notable-date id.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to count.</param>
    /// <returns>The number of matching occurrences.</returns>
    private static int Count(IReadOnlyList<ResolvedNotableDate> results, string notableDateId) =>
        results.Count(r => r.NotableDateId == notableDateId);
}
