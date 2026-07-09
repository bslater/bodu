// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullRateCacheTests.Create.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class NullRateCacheTests
{
    /// <summary>
    /// Verifies that the factory binds the supplied provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderSupplied_ShouldBindProvider()
    {
        IRateCache cache = NullRateCache.Create("Yahoo");

        Assert.AreEqual("Yahoo", cache.Provider);
    }

    /// <summary>
    /// Verifies that the factory rejects a blank provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NullRateCache.Create("  ");
        });

        Assert.AreEqual("provider", ex.ParamName);
    }
}
