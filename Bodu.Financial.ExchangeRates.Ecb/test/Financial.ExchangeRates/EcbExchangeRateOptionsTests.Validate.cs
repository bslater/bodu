// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="EcbExchangeRateOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        EcbExchangeRateOptions options = new() { Feeds = [] };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });
    }
}
