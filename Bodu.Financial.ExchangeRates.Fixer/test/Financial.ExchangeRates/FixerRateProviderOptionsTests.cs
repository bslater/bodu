// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixerRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the configuration surface and validation of <see cref="FixerRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class FixerRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor sets the Fixer base address and the January 1999 history floor.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetFixerDefaults()
    {
        FixerRateProviderOptions options = new();

        Assert.AreEqual(new Uri("https://data.fixer.io/api/"), options.BaseAddress);
        Assert.AreEqual(RateHistoryAvailability.Since(new DateOnly(1999, 1, 1)), options.HistoryAvailability);
    }
}
