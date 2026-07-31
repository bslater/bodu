// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceAsyncExtensionsTests.ResolveAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateServiceAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that calling with a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly,
    /// before the stream is enumerated.
    /// </summary>
    [TestMethod]
    public void ResolveAsync_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        DateRange range = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((INotableDateService)null!).ResolveAsync(range, "XX");
        });

        Assert.AreEqual("service", ex.ParamName);
    }

    /// <summary>
    /// Verifies that calling with a <see langword="null" /> territory throws <see cref="ArgumentNullException" />
    /// eagerly, before the stream is enumerated.
    /// </summary>
    [TestMethod]
    public void ResolveAsync_WhenTerritoryIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateService service = CommonCatalogues.Service("global-core");
        DateRange range = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.ResolveAsync(range, null!);
        });

        Assert.AreEqual("territory", ex.ParamName);
    }

    /// <summary>
    /// Verifies that calling with a range whose start is later than its end throws
    /// <see cref="ArgumentOutOfRangeException" /> eagerly, matching the synchronous range overload.
    /// </summary>
    [TestMethod]
    public void ResolveAsync_WhenRangeIsInverted_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateService service = CommonCatalogues.Service("global-core");
        DateRange inverted = new(new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = service.ResolveAsync(inverted, CommonCatalogues.NeutralTerritory);
        });
    }

    /// <summary>
    /// Verifies that a multi-year stream yields exactly the occurrences the synchronous range overload returns, in the
    /// same order, including occurrences whose multi-day span crosses a year boundary exactly once.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenMultiYearRange_ShouldMatchSynchronousResolve()
    {
        NotableDateService service = CommonCatalogues.Service("global-jewish");
        DateRange range = new(new DateOnly(2023, 1, 1), new DateOnly(2026, 12, 31));

        List<NotableDate> streamed = await DrainAsync(
            service.ResolveAsync(range, CommonCatalogues.NeutralTerritory));

        IReadOnlyList<NotableDate> synchronous = service.Resolve(range, CommonCatalogues.NeutralTerritory);

        CollectionAssert.AreEqual(AsComparable(synchronous), AsComparable(streamed));
    }

    /// <summary>
    /// Verifies that a range starting and ending mid-year yields exactly the occurrences the synchronous range overload
    /// returns for the same partial-year window.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenRangeStartsAndEndsMidYear_ShouldMatchSynchronousResolve()
    {
        NotableDateService service = CommonCatalogues.Service("global-jewish");
        DateRange range = new(new DateOnly(2023, 6, 15), new DateOnly(2025, 3, 10));

        List<NotableDate> streamed = await DrainAsync(
            service.ResolveAsync(range, CommonCatalogues.NeutralTerritory));

        IReadOnlyList<NotableDate> synchronous = service.Resolve(range, CommonCatalogues.NeutralTerritory);

        CollectionAssert.AreEqual(AsComparable(synchronous), AsComparable(streamed));
    }

    /// <summary>
    /// Verifies that supplying a filter restricts the stream to matching occurrences, identically to the filtered
    /// synchronous range overload.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenFilterSupplied_ShouldMatchSynchronousFilteredResolve()
    {
        NotableDateService service = CommonCatalogues.Service("global-jewish");
        DateRange range = new(new DateOnly(2023, 1, 1), new DateOnly(2025, 12, 31));
        NotableDateFilter filter = NotableDateFilter.WithMinDuration(2);

        List<NotableDate> streamed = await DrainAsync(
            service.ResolveAsync(range, CommonCatalogues.NeutralTerritory, filter));

        IReadOnlyList<NotableDate> synchronous = service.Resolve(range, CommonCatalogues.NeutralTerritory, filter);

        CollectionAssert.AreEqual(AsComparable(synchronous), AsComparable(streamed));
    }

    /// <summary>
    /// Verifies that a multi-year stream yields occurrences in non-decreasing date order with no duplicated occurrence
    /// identity on the same date.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenMultiYearRange_ShouldYieldOrderedDistinctOccurrences()
    {
        NotableDateService service = CommonCatalogues.Service("global-jewish");
        DateRange range = new(new DateOnly(2020, 1, 1), new DateOnly(2026, 12, 31));

        List<NotableDate> streamed = await DrainAsync(
            service.ResolveAsync(range, CommonCatalogues.NeutralTerritory));

        for (int i = 1; i < streamed.Count; i++)
        {
            Assert.IsTrue(streamed[i - 1].Date <= streamed[i].Date, $"Out-of-order date at index {i}.");
        }

        var identities = AsComparable(streamed);
        Assert.AreEqual(identities.Count, identities.Distinct().Count(), "Stream yielded a duplicated occurrence.");
    }

    /// <summary>
    /// Verifies that a stream enumerated with an already-cancelled token throws
    /// <see cref="OperationCanceledException" /> before yielding any occurrence.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenTokenAlreadyCancelled_ShouldThrowOperationCanceledException()
    {
        NotableDateService service = CommonCatalogues.Service("global-core");
        DateRange range = new(new DateOnly(2024, 1, 1), new DateOnly(2026, 12, 31));
        using CancellationTokenSource source = new();
        await source.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NotableDate _ in service.ResolveAsync(range, CommonCatalogues.NeutralTerritory, cancellationToken: source.Token))
            {
                Assert.Fail("The stream yielded despite a cancelled token.");
            }
        });
    }

    /// <summary>
    /// Verifies that cancelling after the first resolved year stops the stream at the next year boundary with
    /// <see cref="OperationCanceledException" />, after having yielded the first year's occurrences.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenCancelledMidStream_ShouldStopAtNextYearBoundary()
    {
        NotableDateService service = CommonCatalogues.Service("global-jewish");
        DateRange range = new(new DateOnly(2024, 1, 1), new DateOnly(2026, 12, 31));
        using CancellationTokenSource source = new();

        int yielded = 0;

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NotableDate occurrence in service.ResolveAsync(range, CommonCatalogues.NeutralTerritory, cancellationToken: source.Token))
            {
                yielded++;

                if (occurrence.Date.Year == 2024)
                    await source.CancelAsync();
            }
        });

        Assert.IsGreaterThan(0, yielded, "The stream cancelled before yielding the first year.");
    }

    /// <summary>
    /// Verifies that a token supplied through <c>WithCancellation</c> cancels the stream, exercising the
    /// <see cref="System.Runtime.CompilerServices.EnumeratorCancellationAttribute" /> combination path.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenTokenSuppliedViaWithCancellation_ShouldThrowOperationCanceledException()
    {
        NotableDateService service = CommonCatalogues.Service("global-core");
        DateRange range = new(new DateOnly(2024, 1, 1), new DateOnly(2026, 12, 31));
        using CancellationTokenSource source = new();
        await source.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NotableDate _ in service.ResolveAsync(range, CommonCatalogues.NeutralTerritory).WithCancellation(source.Token))
            {
                Assert.Fail("The stream yielded despite a cancelled token.");
            }
        });
    }

    /// <summary>
    /// Verifies that a single-day range streams the same occurrences as the synchronous single-day overload reports for
    /// that day.
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WhenSingleDayRange_ShouldMatchSynchronousResolve()
    {
        NotableDateService service = CommonCatalogues.Service("global-core");
        DateOnly day = new(2026, 1, 1);
        DateRange range = new(day, day);

        List<NotableDate> streamed = await DrainAsync(
            service.ResolveAsync(range, CommonCatalogues.NeutralTerritory));

        IReadOnlyList<NotableDate> synchronous = service.Resolve(range, CommonCatalogues.NeutralTerritory);

        CollectionAssert.AreEqual(AsComparable(synchronous), AsComparable(streamed));
    }
}
