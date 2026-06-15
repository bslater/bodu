// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Verifies the validation rules of <see cref="DistributedExchangeRateCacheOptions" /> and the argument validation of
/// the <see cref="DistributedExchangeRateCache" /> constructors.
/// </summary>
[TestClass]
public sealed class DistributedExchangeRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that validation rejects options whose provider is blank.
    /// </summary>
    [TestMethod]
    public void Validate_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "  " };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => options.Validate(),
            "Provider");
    }

    /// <summary>
    /// Verifies that validation rejects a key prefix that is supplied but consists only of white space.
    /// </summary>
    [TestMethod]
    public void Validate_WhenKeyPrefixIsWhiteSpace_ShouldThrowArgumentException()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "RBA", KeyPrefix = "   " };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => options.Validate(),
            "KeyPrefix");
    }

    /// <summary>
    /// Verifies that validation accepts options without a key prefix.
    /// </summary>
    [TestMethod]
    public void Validate_WhenNoKeyPrefixSupplied_ShouldNotThrow()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "RBA" };

        options.Validate();
    }

    /// <summary>
    /// Verifies that validation accepts options with a non-blank key prefix.
    /// </summary>
    [TestMethod]
    public void Validate_WhenKeyPrefixSupplied_ShouldNotThrow()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "RBA", KeyPrefix = "fx:" };

        options.Validate();
    }

    /// <summary>
    /// Verifies that constructing a cache with a <see langword="null" /> distributed cache throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDistributedCacheIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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
