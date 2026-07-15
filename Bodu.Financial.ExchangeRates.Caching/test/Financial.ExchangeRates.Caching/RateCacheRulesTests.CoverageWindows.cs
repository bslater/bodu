// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheRulesTests.CoverageWindows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCacheRulesTests
{
    /// <summary>
    /// Builds a coverage window over June 2026 days.
    /// </summary>
    /// <param name="startDay">The inclusive first day-of-month.</param>
    /// <param name="endDay">The inclusive last day-of-month.</param>
    /// <param name="ageHours">The window's fetch age in hours relative to <see cref="Now" />.</param>
    /// <returns>The coverage window.</returns>
    private static CoverageWindow Window(int startDay, int endDay, int ageHours = 1) =>
        new(new DateOnly(2026, 6, startDay), new DateOnly(2026, 6, endDay), Now - TimeSpan.FromHours(ageHours));

    /// <summary>
    /// Projects windows into the tuple shape the public rule overloads accept.
    /// </summary>
    /// <param name="windows">The windows to project.</param>
    /// <returns>The equivalent tuples.</returns>
    private static List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> AsTuples(IReadOnlyList<CoverageWindow> windows) =>
        windows.Select(w => (w.Start, w.End, w.FetchedAtUtc)).ToList();

    /// <summary>
    /// Verifies that the window-typed coverage fold produces the same coverage as the public tuple overload over a
    /// mix of fresh and stale windows.
    /// </summary>
    [TestMethod]
    public void BuildCoverage_WhenWindowTyped_ShouldMatchTupleOverload()
    {
        IReadOnlyList<CoverageWindow> windows = [Window(1, 5), Window(10, 12, ageHours: 48), Window(15, 20)];

        DateRangeCoverage fromWindows = RateCacheRules.BuildCoverage(windows, Duration, Now);
        DateRangeCoverage fromTuples = RateCacheRules.BuildCoverage(AsTuples(windows), Duration, Now);

        for (int day = 1; day <= 25; day++)
        {
            var date = new DateOnly(2026, 6, day);
            Assert.AreEqual(
                fromTuples.Contains(date, date),
                fromWindows.Contains(date, date),
                $"coverage for {date} must match the tuple overload");
        }
    }

    /// <summary>
    /// Verifies that the window-typed merge prunes stale windows, drops windows fully superseded by the new range,
    /// keeps partially overlapping windows, and appends the new window — identically to the public tuple overload.
    /// </summary>
    [TestMethod]
    public void MergeCoverage_WhenWindowTyped_ShouldMatchTupleOverload()
    {
        // A stale window (pruned), a fully contained fresh window (superseded), a partially overlapping fresh window
        // (kept), and a disjoint fresh window (kept).
        IReadOnlyList<CoverageWindow> existing =
        [
            Window(1, 3, ageHours: 48),
            Window(11, 13),
            Window(8, 12),
            Window(25, 28),
        ];
        var start = new DateOnly(2026, 6, 10);
        var end = new DateOnly(2026, 6, 15);

        List<CoverageWindow> merged = RateCacheRules.MergeCoverage(existing, start, end, Duration, Now);
        List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> expected =
            RateCacheRules.MergeCoverage(AsTuples(existing), start, end, Duration, Now);

        Assert.HasCount(expected.Count, merged);
        for (int i = 0; i < merged.Count; i++)
        {
            Assert.AreEqual(expected[i].Start, merged[i].Start);
            Assert.AreEqual(expected[i].End, merged[i].End);
            Assert.AreEqual(expected[i].FetchedAtUtc, merged[i].FetchedAtUtc);
        }
    }

    /// <summary>
    /// Verifies that the window-typed merge stamps the appended window with the evaluation instant and rejects an
    /// inverted range, matching the public overload's contract.
    /// </summary>
    [TestMethod]
    public void MergeCoverage_WhenWindowTypedRangeInverted_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = RateCacheRules.MergeCoverage(
                Array.Empty<CoverageWindow>(), new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 5), Duration, Now);
        });

        Assert.AreEqual("start", ex.ParamName);
    }

    /// <summary>
    /// Verifies that merging into an empty window set yields exactly the new window stamped at the evaluation instant.
    /// </summary>
    [TestMethod]
    public void MergeCoverage_WhenWindowTypedAndExistingEmpty_ShouldReturnOnlyNewWindow()
    {
        List<CoverageWindow> merged = RateCacheRules.MergeCoverage(
            Array.Empty<CoverageWindow>(), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), Duration, Now);

        Assert.HasCount(1, merged);
        Assert.AreEqual(new DateOnly(2026, 6, 1), merged[0].Start);
        Assert.AreEqual(new DateOnly(2026, 6, 5), merged[0].End);
        Assert.AreEqual(Now, merged[0].FetchedAtUtc);
    }
}
