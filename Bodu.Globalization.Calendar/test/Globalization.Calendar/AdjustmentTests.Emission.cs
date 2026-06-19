// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTests.Emission.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTests
{
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
}
