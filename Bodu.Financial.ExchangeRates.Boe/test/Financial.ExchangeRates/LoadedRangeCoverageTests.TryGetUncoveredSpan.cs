// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedRangeCoverageTests.TryGetUncoveredSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class LoadedRangeCoverageTests
{
    /// <summary>
    /// Verifies that a window extending a range held for good reports only the days after it.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenWindowExtendsFinalRange_ShouldReturnTheNewDays()
    {
        LoadedRangeCoverage coverage = new();
        coverage.Add(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 10), null, s_fetchedAt, s_refresh);

        bool found = coverage.TryGetUncoveredSpan(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 12), s_fetchedAt, out DateOnly start, out DateOnly end);

        Assert.IsTrue(found);
        Assert.AreEqual(new DateOnly(2022, 12, 11), start);
        Assert.AreEqual(new DateOnly(2022, 12, 12), end);
    }

    /// <summary>
    /// Verifies that provisionally held days count as held until they expire, and as missing after.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenProvisionalDaysExpire_ShouldReturnThem()
    {
        LoadedRangeCoverage coverage = new();
        coverage.Add(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 3), s_fetchedAt, s_refresh);

        bool before = coverage.TryGetUncoveredSpan(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), s_fetchedAt.AddHours(1), out _, out _);
        bool after = coverage.TryGetUncoveredSpan(new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4), s_fetchedAt.AddHours(13), out DateOnly start, out DateOnly end);

        Assert.IsFalse(before);
        Assert.IsTrue(after);
        Assert.AreEqual(new DateOnly(2023, 1, 4), start);
        Assert.AreEqual(new DateOnly(2023, 1, 4), end);
    }

    /// <summary>
    /// Verifies that a window with gaps reports one span from its first missing day to its last.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenWindowHasGaps_ShouldSpanFromFirstToLastMissingDay()
    {
        LoadedRangeCoverage coverage = new();
        coverage.Add(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 3), null, s_fetchedAt, s_refresh);
        coverage.Add(new DateOnly(2022, 12, 6), new DateOnly(2022, 12, 7), null, s_fetchedAt, s_refresh);
        coverage.Add(new DateOnly(2022, 12, 10), new DateOnly(2022, 12, 12), null, s_fetchedAt, s_refresh);

        _ = coverage.TryGetUncoveredSpan(new DateOnly(2022, 12, 1), new DateOnly(2022, 12, 12), s_fetchedAt, out DateOnly start, out DateOnly end);

        Assert.AreEqual(new DateOnly(2022, 12, 4), start);
        Assert.AreEqual(new DateOnly(2022, 12, 9), end);
    }

    /// <summary>
    /// Verifies that a window whose start is later than its end throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void TryGetUncoveredSpan_WhenStartIsAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        LoadedRangeCoverage coverage = new();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = coverage.TryGetUncoveredSpan(new DateOnly(2023, 1, 5), new DateOnly(2023, 1, 4), s_fetchedAt, out _, out _);
        });
    }
}
