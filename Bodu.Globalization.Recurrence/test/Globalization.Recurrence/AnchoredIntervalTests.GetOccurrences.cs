// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.GetOccurrences.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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

    /// <summary>
    /// Verifies that the offset window overload yields exactly the occurrences, in the anchor's offset, that a scan of
    /// the unbounded stream from the first occurrence finds in the window, for windows far from the anchor, before it,
    /// across it, and in offsets that differ from it.
    /// </summary>
    /// <param name="intervalSeconds">The interval, in seconds.</param>
    /// <param name="anchorText">The anchor, in round-trip text.</param>
    /// <param name="fromText">The inclusive lower bound of the window, in round-trip text.</param>
    /// <param name="toText">The inclusive upper bound of the window, in round-trip text.</param>
    [TestMethod]
    [DataRow(420, "1990-03-04T05:06:07+10:00", "2026-01-01T00:00:00-05:00", "2026-01-01T02:00:00+00:00", DisplayName = "seven minutes, decades after the anchor")]
    [DataRow(86_400, "1900-01-01T09:00:00+01:00", "2026-06-01T00:00:00+14:00", "2026-06-10T00:00:00-12:00", DisplayName = "one day, a century after the anchor")]
    [DataRow(3_600, "2000-02-29T23:30:00-03:30", "2026-03-08T01:00:00-05:00", "2026-03-08T07:00:00-04:00", DisplayName = "one hour, bounds in different offsets")]
    [DataRow(604_800, "1950-01-02T00:00:00+00:00", "2026-01-01T00:00:00+00:00", "2026-03-01T00:00:00+00:00", DisplayName = "one week, decades after the anchor")]
    [DataRow(3_600, "2026-01-01T00:00:00+00:00", "2025-01-01T00:00:00+00:00", "2025-12-31T23:00:00+00:00", DisplayName = "window before the anchor")]
    [DataRow(3_600, "2026-01-01T00:00:00+05:00", "2025-12-31T00:00:00+00:00", "2026-01-01T06:00:00+00:00", DisplayName = "window across the anchor")]
    [DataRow(3_600, "2026-01-01T00:00:00+00:00", "2026-01-02T03:00:00+02:00", "2026-01-02T03:00:00+02:00", DisplayName = "window of one instant on an occurrence")]
    public void GetOccurrences_WhenOffsetWindowIsFarFromAnchor_ShouldMatchTheScan(
        int intervalSeconds,
        string anchorText,
        string fromText,
        string toText)
    {
        var interval = new AnchoredInterval(TimeSpan.FromSeconds(intervalSeconds));
        var anchor = DateTimeOffset.Parse(anchorText, CultureInfo.InvariantCulture);
        var from = DateTimeOffset.Parse(fromText, CultureInfo.InvariantCulture);
        var to = DateTimeOffset.Parse(toText, CultureInfo.InvariantCulture);

        string[] expected = interval
            .GetOccurrences(anchor)
            .SkipWhile(occurrence => occurrence < from)
            .TakeWhile(occurrence => occurrence <= to)
            .Select(occurrence => occurrence.ToString("O", CultureInfo.InvariantCulture))
            .ToArray();

        string[] actual = interval
            .GetOccurrences(anchor, from, to)
            .Select(occurrence => occurrence.ToString("O", CultureInfo.InvariantCulture))
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the offset window overload starts at the first occurrence in the window without walking the
    /// occurrences before it, so a one-second interval anchored millennia before the window answers promptly.
    /// </summary>
    /// <returns>A task that completes when the verification has finished.</returns>
    [TestMethod]
    public async Task GetOccurrences_WhenOffsetWindowIsMillenniaFromAnchor_ShouldStartAtTheWindow()
    {
        var interval = new AnchoredInterval(TimeSpan.FromSeconds(1));
        var anchor = new DateTimeOffset(1, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var from = new DateTimeOffset(9000, 1, 1, 10, 0, 0, TimeSpan.FromHours(10));
        var to = new DateTimeOffset(9000, 1, 1, 0, 0, 2, TimeSpan.Zero);

        // A scan from the first occurrence would walk about 2.8e11 occurrences before the window; the bound is
        // generous, so only a scan can exceed it.
        using var timeout = new CancellationTokenSource();
        Task<DateTimeOffset[]> enumeration = Task.Run(() => interval.GetOccurrences(anchor, from, to).ToArray());
        Task completed = await Task.WhenAny(enumeration, Task.Delay(TimeSpan.FromSeconds(30), timeout.Token));
        await timeout.CancelAsync();

        Assert.AreSame(enumeration, completed, "The window did not start within 30 seconds.");
        CollectionAssert.AreEqual(
            new[]
            {
                new DateTimeOffset(9000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(9000, 1, 1, 0, 0, 1, TimeSpan.Zero),
                new DateTimeOffset(9000, 1, 1, 0, 0, 2, TimeSpan.Zero),
            },
            await enumeration);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> occurrence stream ends at the last occurrence whose UTC instant a
    /// <see cref="DateTimeOffset" /> can hold, as documented, rather than throwing on the first one past it.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenAnchorHasOffset_AndOccurrencesPassTheLastUtcInstant_ShouldEndAtTheLastRepresentable()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(1));
        var offset = TimeSpan.FromHours(-5);
        var anchor = new DateTimeOffset(9999, 12, 31, 15, 0, 0, offset);

        DateTimeOffset[] actual = interval.GetOccurrences(anchor).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTimeOffset(9999, 12, 31, 16, 0, 0, offset),
                new DateTimeOffset(9999, 12, 31, 17, 0, 0, offset),
                new DateTimeOffset(9999, 12, 31, 18, 0, 0, offset),
            },
            actual);
    }

    /// <summary>
    /// Verifies that the windowed <see cref="DateTimeOffset" /> overload, closed at
    /// <see cref="DateTimeOffset.MaxValue" />, ends at the last occurrence whose UTC instant a
    /// <see cref="DateTimeOffset" /> can hold, rather than throwing on the first one past it.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenAnchorHasOffset_AndWindowedToTheLastOffsetInstant_ShouldEndAtTheLastRepresentable()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(1));
        var offset = TimeSpan.FromHours(-5);
        var anchor = new DateTimeOffset(9999, 12, 31, 15, 0, 0, offset);

        DateTimeOffset[] actual = interval
            .GetOccurrences(anchor, new DateTimeOffset(9999, 12, 31, 17, 0, 0, offset), DateTimeOffset.MaxValue)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTimeOffset(9999, 12, 31, 17, 0, 0, offset),
                new DateTimeOffset(9999, 12, 31, 18, 0, 0, offset),
            },
            actual);
    }
}
