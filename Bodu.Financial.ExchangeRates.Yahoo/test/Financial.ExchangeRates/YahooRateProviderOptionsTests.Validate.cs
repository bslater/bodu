// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class YahooRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="YahooRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        YahooRateProviderOptions options = new() { ChartPath = "no-placeholder" };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
