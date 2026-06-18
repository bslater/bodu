// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullExchangeRateCacheTests.Create.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class NullExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that the factory binds the supplied provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderSupplied_ShouldBindProvider()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");

        Assert.AreEqual("Yahoo", cache.Provider);
    }

    /// <summary>
    /// Verifies that the factory rejects a blank provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NullExchangeRateCache.Create("  ");
        });

        Assert.AreEqual("provider", ex.ParamName);
    }
}
