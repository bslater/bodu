// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the validation and default behaviour of <see cref="OandaRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class OandaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the default options declare a 180-day rolling history availability, matching the anonymous OANDA
    /// endpoint's coverage.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefault_ShouldBeRolling180Days()
    {
        OandaRateProviderOptions options = new();

        Assert.AreEqual(RateHistoryAvailabilityKind.Rolling, options.HistoryAvailability.Kind);
        Assert.AreEqual(180, options.HistoryAvailability.WindowDays);
    }
}
