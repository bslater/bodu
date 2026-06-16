// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.Disposal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="CachingExchangeRateProvider" /> disposes its inner provider only when constructed to own it.
/// </summary>
public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that disposing the decorator disposes a disposable inner provider when <c>ownsInner</c> is
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenOwnsInnerTrue_ShouldDisposeInner()
    {
        DisposableProbeProvider inner = new();
        var sut = new CachingExchangeRateProvider(inner, _cache, _options, _clock, ownsInner: true);

        sut.Dispose();

        Assert.AreEqual(1, inner.DisposeCount);
    }

    /// <summary>
    /// Verifies that disposing the decorator leaves the inner provider untouched by default, so an inner owned elsewhere
    /// (for example by the dependency-injection container) is not disposed twice.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenOwnsInnerFalse_ShouldNotDisposeInner()
    {
        DisposableProbeProvider inner = new();
        var sut = new CachingExchangeRateProvider(inner, _cache, _options, _clock);

        sut.Dispose();

        Assert.AreEqual(0, inner.DisposeCount);
    }

    /// <summary>
    /// Verifies that disposing an owning decorator more than once disposes the inner provider only once.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwiceAndOwnsInner_ShouldDisposeInnerOnce()
    {
        DisposableProbeProvider inner = new();
        var sut = new CachingExchangeRateProvider(inner, _cache, _options, _clock, ownsInner: true);

        sut.Dispose();
        sut.Dispose();

        Assert.AreEqual(1, inner.DisposeCount);
    }
}
