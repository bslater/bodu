// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxExchangeRateOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class OfxExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="OfxExchangeRateOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        OfxExchangeRateOptions options = new() { ReportingInterval = string.Empty };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });
    }
}
