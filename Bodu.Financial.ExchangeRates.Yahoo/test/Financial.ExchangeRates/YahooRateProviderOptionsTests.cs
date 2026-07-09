// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the validation behaviour of <see cref="YahooRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class YahooRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the default options advertise a fixed history floor at the December 2003 inception of Yahoo's
    /// foreign-exchange chart data.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefault_ShouldBeSinceDecember2003()
    {
        YahooRateProviderOptions options = new();

        Assert.AreEqual(RateHistoryAvailabilityKind.Since, options.HistoryAvailability.Kind);
        Assert.AreEqual(new DateOnly(2003, 12, 1), options.HistoryAvailability.EarliestDate);
    }
}
