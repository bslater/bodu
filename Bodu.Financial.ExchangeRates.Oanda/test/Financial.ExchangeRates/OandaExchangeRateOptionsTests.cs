// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaExchangeRateOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the validation and default behaviour of <see cref="OandaExchangeRateOptions" />.
/// </summary>
[TestClass]
public partial class OandaExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the default options declare a 180-day rolling history availability, matching the anonymous OANDA
    /// endpoint's coverage.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefault_ShouldBeRolling180Days()
    {
        OandaExchangeRateOptions options = new();

        Assert.AreEqual(RateHistoryAvailabilityKind.Rolling, options.HistoryAvailability.Kind);
        Assert.AreEqual(180, options.HistoryAvailability.WindowDays);
    }
}
