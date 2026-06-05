// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LunarPhaseAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the on-or-after lunar phase search helpers exposed by the internal
/// <see cref="LunarPhaseAlgorithm" />. The algorithm is documented at approximately ±2 minutes of phase
/// transition, so consecutive-month spacing and the on-or-after contract are the primary observable
/// invariants exercised here.
/// </summary>
[TestClass]
public sealed class LunarPhaseAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="LunarPhaseAlgorithm.GetFullMoonOnOrAfter" /> never returns a date earlier than the
    /// requested search anchor (the "on or after" contract).
    /// </summary>
    [TestMethod]
    public void GetFullMoonOnOrAfter_ShouldNeverReturnDateEarlierThanSearchAnchor()
    {
        DateTime anchor = new(2026, 3, 15);

        DateTime? actual = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(anchor);

        Assert.IsNotNull(actual);
        Assert.IsTrue(actual!.Value >= anchor.Date,
            $"GetFullMoonOnOrAfter returned {actual.Value:yyyy-MM-dd} earlier than anchor {anchor:yyyy-MM-dd}.");
    }

    /// <summary>
    /// Verifies that <see cref="LunarPhaseAlgorithm.GetFullMoonOnOrAfter" /> returns a date within a single synodic month
    /// of the anchor — full moons recur every ≈29.53 days, so the first full moon on or after any date must arrive within
    /// 30 days.
    /// </summary>
    [TestMethod]
    public void GetFullMoonOnOrAfter_ShouldReturnDateWithinOneSynodicMonthOfAnchor()
    {
        DateTime anchor = new(2026, 1, 1);

        DateTime? actual = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(anchor);

        Assert.IsNotNull(actual);
        var diff = (actual!.Value - anchor.Date).Days;
        Assert.IsTrue(diff is >= 0 and <= 31,
            $"Expected the next full moon within 31 days of the anchor, got {diff} days.");
    }

    /// <summary>
    /// Verifies that <see cref="LunarPhaseAlgorithm.GetNewMoonOnOrAfter" /> never returns a date earlier than the
    /// requested search anchor.
    /// </summary>
    [TestMethod]
    public void GetNewMoonOnOrAfter_ShouldNeverReturnDateEarlierThanSearchAnchor()
    {
        DateTime anchor = new(2026, 5, 1);

        DateTime? actual = LunarPhaseAlgorithm.GetNewMoonOnOrAfter(anchor);

        Assert.IsNotNull(actual);
        Assert.IsTrue(actual!.Value >= anchor.Date,
            $"GetNewMoonOnOrAfter returned {actual.Value:yyyy-MM-dd} earlier than anchor {anchor:yyyy-MM-dd}.");
    }

    /// <summary>
    /// Verifies that <see cref="LunarPhaseAlgorithm.GetNewMoonOnOrAfter" /> returns a date within one synodic month of
    /// the anchor.
    /// </summary>
    [TestMethod]
    public void GetNewMoonOnOrAfter_ShouldReturnDateWithinOneSynodicMonthOfAnchor()
    {
        DateTime anchor = new(2026, 1, 1);

        DateTime? actual = LunarPhaseAlgorithm.GetNewMoonOnOrAfter(anchor);

        Assert.IsNotNull(actual);
        var diff = (actual!.Value - anchor.Date).Days;
        Assert.IsTrue(diff is >= 0 and <= 31,
            $"Expected the next new moon within 31 days of the anchor, got {diff} days.");
    }

    /// <summary>
    /// Verifies that consecutive calls to <see cref="LunarPhaseAlgorithm.GetNewMoonOnOrAfter" /> with the second anchor
    /// just after the first result produce a date approximately one synodic month later.
    /// </summary>
    [TestMethod]
    public void GetNewMoonOnOrAfter_WhenChained_ShouldAdvanceByOneSynodicMonth()
    {
        DateTime anchor = new(2026, 1, 1);

        DateTime? first = LunarPhaseAlgorithm.GetNewMoonOnOrAfter(anchor);
        Assert.IsNotNull(first);

        DateTime? second = LunarPhaseAlgorithm.GetNewMoonOnOrAfter(first!.Value.AddDays(1));
        Assert.IsNotNull(second);

        var diff = (second!.Value - first.Value).Days;
        Assert.IsTrue(diff is >= 28 and <= 31,
            $"Expected consecutive new moons 28..31 days apart, got {diff} days.");
    }

    /// <summary>
    /// Verifies that consecutive calls to <see cref="LunarPhaseAlgorithm.GetFullMoonOnOrAfter" /> with the second anchor
    /// just after the first result produce a date approximately one synodic month later.
    /// </summary>
    [TestMethod]
    public void GetFullMoonOnOrAfter_WhenChained_ShouldAdvanceByOneSynodicMonth()
    {
        DateTime anchor = new(2026, 2, 1);

        DateTime? first = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(anchor);
        Assert.IsNotNull(first);

        DateTime? second = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(first!.Value.AddDays(1));
        Assert.IsNotNull(second);

        var diff = (second!.Value - first.Value).Days;
        Assert.IsTrue(diff is >= 28 and <= 31,
            $"Expected consecutive full moons 28..31 days apart, got {diff} days.");
    }

    /// <summary>
    /// Verifies that <see cref="LunarPhaseAlgorithm.GetFullMoonOnOrAfter" /> ignores the time-of-day component on the
    /// anchor — a search from midday should produce the same result as a search from midnight on the same day.
    /// </summary>
    [TestMethod]
    public void GetFullMoonOnOrAfter_ShouldIgnoreTimeComponentOnAnchor()
    {
        DateTime atMidnight = new(2026, 1, 1, 0, 0, 0);
        DateTime atNoon = new(2026, 1, 1, 12, 0, 0);

        DateTime? withMidnight = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(atMidnight);
        DateTime? withNoon = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(atNoon);

        Assert.AreEqual(withMidnight, withNoon);
    }
}
