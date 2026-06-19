// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentEmissionMatrixTests.Emission.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentEmissionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="EmissionMode.ActualOnly" /> emits only the calculated weekend date with no observed
    /// occurrence, even when the trigger fires. New Year's Day 2022 is a Saturday.
    /// </summary>
    [TestMethod]
    public void Emission_ActualOnly_WhenWeekend_ShouldEmitOnlyActual()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ActualOnly"), 2022);

        Assert.HasCount(1, results);
        Assert.AreEqual(
            (new DateOnly(2022, 1, 1), false, (string?)null),
            (results[0].Date, results[0].IsObserved, results[0].AdjustmentPolicyId));
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.ObservedOnly" /> suppresses the calculated weekend date and emits only the
    /// observed Monday substitute. New Year's Day 2022 (Saturday) is observed on Monday 3 January.
    /// </summary>
    [TestMethod]
    public void Emission_ObservedOnly_WhenWeekend_ShouldEmitOnlyObserved()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ObservedOnly"), 2022);

        Assert.HasCount(1, results);
        Assert.AreEqual(
            (new DateOnly(2022, 1, 3), (DateOnly?)new DateOnly(2022, 1, 1), true, (string?)"mondayise"),
            (results[0].Date, results[0].ActualDate, results[0].IsObserved, results[0].AdjustmentPolicyId));
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.ActualAndObserved" /> emits both the calculated weekend date and the
    /// observed Monday substitute when the trigger fires.
    /// </summary>
    [TestMethod]
    public void Emission_ActualAndObserved_WhenWeekend_ShouldEmitBoth()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ActualAndObserved"), 2022);

        CollectionAssert.AreEqual(
            new[] { (new DateOnly(2022, 1, 1), false), (new DateOnly(2022, 1, 3), true) },
            results.OrderBy(r => r.Date).Select(r => (r.Date, r.IsObserved)).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.ObservedAsAdditional" /> emits the calculated weekend date plus an
    /// additional observed Monday occurrence when the trigger fires.
    /// </summary>
    [TestMethod]
    public void Emission_ObservedAsAdditional_WhenWeekend_ShouldEmitActualPlusObserved()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ObservedAsAdditional"), 2022);

        CollectionAssert.AreEqual(
            new[] { (new DateOnly(2022, 1, 1), false), (new DateOnly(2022, 1, 3), true) },
            results.OrderBy(r => r.Date).Select(r => (r.Date, r.IsObserved)).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.Suppress" /> removes the occurrence entirely when the trigger fires on a
    /// weekend, emitting neither the actual nor any observed date.
    /// </summary>
    [TestMethod]
    public void Emission_Suppress_WhenWeekend_ShouldEmitNothing()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("Suppress"), 2022);

        Assert.IsEmpty(results);
    }

    /// <summary>
    /// Verifies that every emission mode leaves a non-firing weekday occurrence as the single unchanged actual date,
    /// because the trigger does not fire and so no emission rule is engaged. New Year's Day 2026 is a Thursday.
    /// </summary>
    /// <param name="emissionMode">The emission mode under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("ActualOnly")]
    [DataRow("ObservedOnly")]
    [DataRow("ActualAndObserved")]
    [DataRow("ObservedAsAdditional")]
    [DataRow("Suppress")]
    public void Emission_WhenWeekdayAndTriggerDoesNotFire_ShouldEmitUnchangedActual(string emissionMode)
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService(emissionMode), 2026);

        Assert.HasCount(1, results, emissionMode);
        Assert.AreEqual(new DateOnly(2026, 1, 1), results[0].Date);
        Assert.IsFalse(results[0].IsObserved);
        Assert.IsNull(results[0].AdjustmentPolicyId);
    }
}
