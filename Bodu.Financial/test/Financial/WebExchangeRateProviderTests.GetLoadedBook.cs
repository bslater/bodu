// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProviderTests.GetLoadedBook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class WebExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a cold provider returns an empty book: the accessor never returns <see langword="null" />, even
    /// before the first fetch.
    /// </summary>
    [TestMethod]
    public void GetLoadedBook_WhenCold_ShouldReturnEmptyBook()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        ExchangeRateBook book = provider.GetLoadedBook();

        Assert.AreEqual(0, book.Count);
    }

    /// <summary>
    /// Verifies that the book returned after a load contains the fetched series.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedBook_AfterLoad_ShouldContainLoadedSeries()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        ExchangeRateBook book = provider.GetLoadedBook();

        Assert.AreEqual(1, book.Count);
        ExchangeRateSeries series = book.EnumerateSeries().Single();
        Assert.AreEqual(Known, series.GetObservations().Single().Date);
    }

    /// <summary>
    /// Verifies that a book handed out before a load is pinned: the later load swaps the provider's internal book and
    /// never mutates the instance already returned.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedBook_WhenLoadedAfterAccess_ShouldNotMutateReturnedBook()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        ExchangeRateBook before = provider.GetLoadedBook();
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);
        ExchangeRateBook after = provider.GetLoadedBook();

        Assert.AreEqual(0, before.Count, "the pre-load book is immutable and unaffected by the fetch");
        Assert.AreEqual(1, after.Count);
        Assert.AreNotSame(before, after);
    }

    /// <summary>
    /// Verifies that reading the book from a disposed provider throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GetLoadedBook_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();
        provider.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = provider.GetLoadedBook();
        });
    }
}
