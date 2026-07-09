// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the validation behaviour of <see cref="XeRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class XeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the default options advertise the estimated ten-year rolling window of XE's server-determined
    /// charting range.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefault_ShouldBeRollingTenYears()
    {
        XeRateProviderOptions options = new();

        Assert.AreEqual(RateHistoryAvailabilityKind.Rolling, options.HistoryAvailability.Kind);
        Assert.AreEqual(3650, options.HistoryAvailability.WindowDays);
    }
}
