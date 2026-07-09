// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderTests.GetLoadedSnapshot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderTests
{
    /// <summary>
    /// Verifies that a cold provider's snapshot resolves no rates: every lookup misses until the first fetch.
    /// </summary>
    [TestMethod]
    public void GetLoadedSnapshot_WhenCold_ShouldResolveNothing()
    {
        TestBulkWebRateProvider provider = CreateProvider();

        FixedDatedRateProvider snapshot = provider.GetLoadedSnapshot();

        Assert.IsFalse(snapshot.TryGetRate("AUD", "USD", Known, RateLookupOptions.Exact, out _));
    }

    /// <summary>
    /// Verifies that the snapshot returned after a load serves the loaded rate, independently of the live provider.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedSnapshot_AfterLoad_ShouldServeLoadedRate()
    {
        TestBulkWebRateProvider provider = CreateProvider();
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        FixedDatedRateProvider snapshot = provider.GetLoadedSnapshot();
        RateLookupResult result = snapshot.GetRate("AUD", "USD", Known, RateLookupOptions.Exact);

        Assert.AreEqual(0.6828m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a snapshot handed out before a load is pinned: the later load swaps the provider's internal
    /// snapshot and never mutates the instance already returned.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedSnapshot_WhenLoadedAfterAccess_ShouldNotMutateReturnedSnapshot()
    {
        TestBulkWebRateProvider provider = CreateProvider();

        FixedDatedRateProvider before = provider.GetLoadedSnapshot();
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.IsFalse(before.TryGetRate("AUD", "USD", Known, RateLookupOptions.Exact, out _), "the pre-load snapshot is immutable and unaffected by the fetch");
        Assert.AreNotSame(before, provider.GetLoadedSnapshot());
    }

    /// <summary>
    /// Verifies that the snapshot exposes the same book instance <see cref="WebRateProvider.GetLoadedBook" />
    /// returns at the same moment, so the two accessors describe one generation of loaded data.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedSnapshot_ShouldExposeSameBookInstanceAsGetLoadedBook()
    {
        TestBulkWebRateProvider provider = CreateProvider();
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreSame(provider.GetLoadedBook(), provider.GetLoadedSnapshot().Book);
    }

    /// <summary>
    /// Verifies that reading the snapshot from a disposed provider throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GetLoadedSnapshot_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TestBulkWebRateProvider provider = CreateProvider();
        provider.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = provider.GetLoadedSnapshot();
        });
    }
}
