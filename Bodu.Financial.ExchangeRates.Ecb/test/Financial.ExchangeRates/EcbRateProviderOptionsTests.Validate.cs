// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="EcbRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        EcbRateProviderOptions options = new() { Feeds = [] };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }

    /// <summary>
    /// Verifies that <see cref="WebRateProviderOptions.Validate" /> throws <see cref="ArgumentException" /> for a missing
    /// base address.
    /// </summary>
    [TestMethod]
    public void Validate_WhenBaseAddressIsNull_ShouldThrowArgumentException()
    {
        EcbRateProviderOptions options = new() { BaseAddress = null! };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });
    }
}
