// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedDownloadTests.GetOrDownloadAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class CachedDownloadTests
{
    /// <summary>
    /// Verifies that a fresh cache entry is returned with its stored time, without building the address or issuing a
    /// request.
    /// </summary>
    [TestMethod]
    public async Task GetOrDownloadAsync_WhenCached_ShouldReturnTheEntryWithoutARequest()
    {
        RecordingCache cache = new();
        cache.Seed("unit", [1, 2]);
        using StubHttpMessageHandler handler = new([9]);
        using HttpClient client = new(handler);
        int urlCalls = 0;

        (byte[] bytes, DateTimeOffset? cachedAtUtc) = await CachedDownload.GetOrDownloadAsync(
            cache,
            "unit",
            TimeSpan.FromHours(3),
            client,
            () =>
            {
                urlCalls++;
                return s_url;
            },
            CancellationToken.None);

        CollectionAssert.AreEqual(new byte[] { 1, 2 }, bytes);
        Assert.AreEqual(cache.StoredAtUtc, cachedAtUtc);
        Assert.AreEqual(0, handler.RequestCount);
        Assert.AreEqual(0, urlCalls);
        Assert.AreEqual(TimeSpan.FromHours(3), cache.LastRefreshInterval);
    }

    /// <summary>
    /// Verifies that a miss downloads the unit from its address, caches the bytes, and reports no cached time.
    /// </summary>
    [TestMethod]
    public async Task GetOrDownloadAsync_WhenNotCached_ShouldDownloadAndCacheTheBytes()
    {
        RecordingCache cache = new();
        using StubHttpMessageHandler handler = new([9, 8]);
        using HttpClient client = new(handler);

        (byte[] bytes, DateTimeOffset? cachedAtUtc) = await CachedDownload.GetOrDownloadAsync(
            cache, "unit", TimeSpan.FromHours(3), client, () => s_url, CancellationToken.None);

        CollectionAssert.AreEqual(new byte[] { 9, 8 }, bytes);
        Assert.IsNull(cachedAtUtc);
        Assert.AreEqual(s_url, handler.LastRequestUri);
        Assert.AreEqual(1, cache.StoreCount);
        CollectionAssert.AreEqual(new byte[] { 9, 8 }, cache.Peek("unit"));
    }

    /// <summary>
    /// Verifies that a failed download propagates as <see cref="HttpRequestException" /> and caches nothing.
    /// </summary>
    [TestMethod]
    public async Task GetOrDownloadAsync_WhenDownloadFails_ShouldThrowAndCacheNothing()
    {
        RecordingCache cache = new();
        using StubHttpMessageHandler handler = new([], HttpStatusCode.ServiceUnavailable);
        using HttpClient client = new(handler);

        _ = await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            _ = await CachedDownload.GetOrDownloadAsync(cache, "unit", TimeSpan.FromHours(3), client, () => s_url, CancellationToken.None);
        });

        Assert.AreEqual(0, cache.StoreCount);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> cache throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void GetOrDownloadAsync_WhenCacheIsNull_ShouldThrowArgumentNullException()
    {
        using HttpClient client = new(new StubHttpMessageHandler([]));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CachedDownload.GetOrDownloadAsync<string>(null!, "unit", TimeSpan.FromHours(3), client, () => s_url, CancellationToken.None).AsTask();
        });

        Assert.AreEqual("cache", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> client throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void GetOrDownloadAsync_WhenHttpClientIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CachedDownload.GetOrDownloadAsync(new RecordingCache(), "unit", TimeSpan.FromHours(3), null!, () => s_url, CancellationToken.None).AsTask();
        });

        Assert.AreEqual("httpClient", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> address builder throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void GetOrDownloadAsync_WhenUrlIsNull_ShouldThrowArgumentNullException()
    {
        using HttpClient client = new(new StubHttpMessageHandler([]));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CachedDownload.GetOrDownloadAsync(new RecordingCache(), "unit", TimeSpan.FromHours(3), client, null!, CancellationToken.None).AsTask();
        });

        Assert.AreEqual("url", ex.ParamName);
    }
}
