// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxRateProviderOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class OfxRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="OfxRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        OfxRateProviderOptions options = new() { ReportingInterval = string.Empty };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
