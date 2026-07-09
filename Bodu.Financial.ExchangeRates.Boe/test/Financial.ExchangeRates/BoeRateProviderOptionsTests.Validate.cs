// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="BoeRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        BoeRateProviderOptions options = new() { Series = [] };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
