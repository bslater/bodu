// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeExchangeRateOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class XeExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="XeExchangeRateOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        XeExchangeRateOptions options = new() { ChartingRatesPath = string.Empty };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }

    /// <summary>
    /// Verifies that <see cref="XeExchangeRateOptions.Validate" /> returns for valid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenValid_ShouldNotThrow()
    {
        XeExchangeRateOptions options = new();

        options.Validate();
    }
}
