// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="RbaRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        RbaRateProviderOptions options = new() { Eras = [] };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
