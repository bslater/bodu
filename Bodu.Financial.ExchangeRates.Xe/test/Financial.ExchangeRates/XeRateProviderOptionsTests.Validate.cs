// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeRateProviderOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class XeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="XeRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        XeRateProviderOptions options = new() { ChartingRatesPath = string.Empty };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }

    /// <summary>
    /// Verifies that <see cref="XeRateProviderOptions.Validate" /> returns for valid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenValid_ShouldNotThrow()
    {
        XeRateProviderOptions options = new();

        options.Validate();
    }
}
