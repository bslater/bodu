// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaRateProviderOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class OandaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="OandaRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        OandaRateProviderOptions options = new() { Price = "spot" };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
