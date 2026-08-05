// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.GetOccurrences.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Verifies that the occurrence stream starts one interval after the anchor and ascends by the interval.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenUnbounded_ShouldStartOneIntervalAfterAnchor()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime[] actual = interval.GetOccurrences(anchor).Take(3).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 1, 12, 0, 0),
                new DateTime(2026, 1, 1, 16, 0, 0),
                new DateTime(2026, 1, 1, 20, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that the windowed overload keeps only occurrences within the inclusive window bounds.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWindowed_ShouldHonorInclusiveBounds()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime[] actual = interval
            .GetOccurrences(anchor, new DateTime(2026, 1, 1, 12, 0, 0), new DateTime(2026, 1, 1, 20, 0, 0))
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 1, 12, 0, 0),
                new DateTime(2026, 1, 1, 16, 0, 0),
                new DateTime(2026, 1, 1, 20, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a window that closes before the first occurrence enumerates empty.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWindowClosesBeforeFirstOccurrence_ShouldBeEmpty()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime[] actual = interval
            .GetOccurrences(anchor, new DateTime(2026, 1, 1, 0, 0, 0), new DateTime(2026, 1, 1, 11, 0, 0))
            .ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that a window far from the anchor starts directly at the first occurrence at or after the lower
    /// bound.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWindowFarFromAnchor_ShouldStartAtLowerBound()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2020, 1, 1, 8, 0, 0);

        DateTime[] actual = interval
            .GetOccurrences(anchor, new DateTime(2026, 1, 1, 13, 0, 0), new DateTime(2026, 1, 1, 21, 0, 0))
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 1, 16, 0, 0),
                new DateTime(2026, 1, 1, 20, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that the offset-based stream carries the anchor's offset on every occurrence.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenAnchorHasOffset_ShouldCarryAnchorOffset()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.FromHours(10));

        DateTimeOffset[] actual = interval.GetOccurrences(anchor).Take(2).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(10)),
                new DateTimeOffset(2026, 1, 1, 16, 0, 0, TimeSpan.FromHours(10)),
            },
            actual);
    }

    /// <summary>
    /// Verifies that offset window bounds supplied in a different offset than the anchor are compared as absolute
    /// instants.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWindowOffsetsDiffer_ShouldCompareInstants()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

        DateTimeOffset[] actual = interval
            .GetOccurrences(
                anchor,
                new DateTimeOffset(2026, 1, 1, 22, 0, 0, TimeSpan.FromHours(10)),
                new DateTimeOffset(2026, 1, 2, 3, 0, 0, TimeSpan.FromHours(10)))
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 1, 16, 0, 0, TimeSpan.Zero),
            },
            actual);
    }
}
