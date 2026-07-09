// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheOptionsTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedExchangeRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that constructing a cache with a <see langword="null" /> distributed cache throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDistributedCacheIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DistributedExchangeRateCache(null!, new DistributedExchangeRateCacheOptions { Provider = "RBA" });
        });

        Assert.AreEqual("cache", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a cache with a <see langword="null" /> options reference throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        IDistributedCache backingStore = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DistributedExchangeRateCache(backingStore, (DistributedExchangeRateCacheOptions)null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the convenience constructor binds the supplied provider into the options.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderOverload_ShouldBindProvider()
    {
        IDistributedCache backingStore = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        var cache = new DistributedExchangeRateCache(backingStore, "RBA");

        Assert.AreEqual("RBA", cache.Provider);
    }
}
