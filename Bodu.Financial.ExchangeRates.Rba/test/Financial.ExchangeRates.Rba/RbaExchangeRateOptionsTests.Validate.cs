// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class RbaExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="RbaExchangeRateOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        RbaExchangeRateOptions options = new() { Eras = [] };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });
    }
}
