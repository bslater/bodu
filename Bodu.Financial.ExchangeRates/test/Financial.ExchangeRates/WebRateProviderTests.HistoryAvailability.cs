// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderTests
{
    /// <summary>
    /// Verifies that the base advertises <see cref="RateHistoryAvailability.Unbounded" /> when a derived
    /// provider does not override the declaration.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenNotOverridden_ShouldReturnUnbounded()
    {
        TestBulkWebRateProvider provider = CreateProvider();

        Assert.AreEqual(RateHistoryAvailability.Unbounded, provider.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that every provider built on <see cref="WebRateProvider" /> is discoverable through the
    /// <see cref="IHistoryAwareRateProvider" /> capability interface, so composing layers can probe it at
    /// runtime.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAccessedThroughInterface_ShouldMatchDirectAccess()
    {
        TestBulkWebRateProvider provider = CreateProvider();

        IHistoryAwareRateProvider aware = provider;

        Assert.AreEqual(provider.HistoryAvailability, aware.HistoryAvailability);
    }
}
