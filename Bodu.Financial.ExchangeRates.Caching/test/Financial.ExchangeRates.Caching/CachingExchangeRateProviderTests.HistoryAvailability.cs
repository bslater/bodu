// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the decorator forwards a history-aware inner provider's advertised availability unchanged: the
    /// cache can only hold what the inner once served, so the decorator is exactly as deep as its source.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenInnerIsHistoryAware_ShouldForwardInnerValue()
    {
        ExchangeRateHistoryAvailability declared = ExchangeRateHistoryAvailability.RollingDays(180);
        HistoryAwareCountingProvider inner = new(declared, []);
        CachingExchangeRateProvider decorator = CreateDecorator(inner);

        Assert.AreEqual(declared, decorator.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that the decorator reflects a change to the inner provider's advertised availability rather than
    /// snapshotting it at construction.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenInnerDeclarationChanges_ShouldReflectCurrentInnerValue()
    {
        HistoryAwareCountingProvider inner = new(ExchangeRateHistoryAvailability.Unbounded, []);
        CachingExchangeRateProvider decorator = CreateDecorator(inner);

        inner.HistoryAvailability = ExchangeRateHistoryAvailability.Since(new DateOnly(2020, 1, 1));

        Assert.AreEqual(ExchangeRateHistoryAvailability.Since(new DateOnly(2020, 1, 1)), decorator.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that wrapping an inner provider that does not implement
    /// <see cref="IHistoryAwareExchangeRateProvider" /> yields
    /// <see cref="ExchangeRateHistoryAvailability.Unbounded" />: a non-aware source declares no floor.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenInnerIsNotHistoryAware_ShouldReturnUnbounded()
    {
        CachingExchangeRateProvider decorator = CreateDecorator(InnerWith());

        Assert.AreEqual(ExchangeRateHistoryAvailability.Unbounded, decorator.HistoryAvailability);
    }
}
