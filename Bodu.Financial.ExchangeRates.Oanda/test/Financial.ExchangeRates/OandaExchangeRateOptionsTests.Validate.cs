// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaExchangeRateOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class OandaExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="OandaExchangeRateOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        OandaExchangeRateOptions options = new() { Price = "spot" };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
