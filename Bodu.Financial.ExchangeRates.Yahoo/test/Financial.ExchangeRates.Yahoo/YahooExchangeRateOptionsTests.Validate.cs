// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

public partial class YahooExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="YahooExchangeRateOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        YahooExchangeRateOptions options = new() { ChartPath = "no-placeholder" };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });
    }
}
