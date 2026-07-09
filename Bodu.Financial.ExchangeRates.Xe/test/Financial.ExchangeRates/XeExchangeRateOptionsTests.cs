// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeExchangeRateOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the validation behaviour of <see cref="XeExchangeRateOptions" />.
/// </summary>
[TestClass]
public partial class XeExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the default options advertise the estimated ten-year rolling window of XE's server-determined
    /// charting range.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefault_ShouldBeRollingTenYears()
    {
        XeExchangeRateOptions options = new();

        Assert.AreEqual(ExchangeRateHistoryAvailabilityKind.Rolling, options.HistoryAvailability.Kind);
        Assert.AreEqual(3650, options.HistoryAvailability.WindowDays);
    }
}
