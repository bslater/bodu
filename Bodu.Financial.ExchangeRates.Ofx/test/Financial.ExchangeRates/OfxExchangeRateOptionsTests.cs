// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxExchangeRateOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the validation behaviour of <see cref="OfxExchangeRateOptions" />.
/// </summary>
[TestClass]
public partial class OfxExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the default options deliberately advertise an unbounded history — OFX publishes multi-decade
    /// data with no fixed inception date.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefault_ShouldBeUnbounded()
    {
        OfxExchangeRateOptions options = new();

        Assert.AreEqual(ExchangeRateHistoryAvailabilityKind.Unbounded, options.HistoryAvailability.Kind);
    }
}
