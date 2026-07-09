// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the validation behaviour of <see cref="OfxRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class OfxRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the default options deliberately advertise an unbounded history — OFX publishes multi-decade
    /// data with no fixed inception date.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefault_ShouldBeUnbounded()
    {
        OfxRateProviderOptions options = new();

        Assert.AreEqual(RateHistoryAvailabilityKind.Unbounded, options.HistoryAvailability.Kind);
    }
}
