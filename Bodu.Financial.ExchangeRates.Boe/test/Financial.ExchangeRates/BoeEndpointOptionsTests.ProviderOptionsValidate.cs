// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeEndpointOptionsTests.ProviderOptionsValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the provider options surface a missing endpoint base URL through their aggregate validation.
    /// </summary>
    [TestMethod]
    public void ProviderOptionsValidate_WhenEndpointBaseUrlIsNull_ShouldThrowArgumentException()
    {
        BoeExchangeRateOptions options = new();
        options.Endpoint.BaseUrl = null!;

        _ = Assert.ThrowsExactly<ArgumentException>(() => options.Validate());
    }
}
