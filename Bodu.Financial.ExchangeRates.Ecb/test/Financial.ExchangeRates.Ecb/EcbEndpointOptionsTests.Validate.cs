// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbEndpointOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

public partial class EcbEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the options reject a missing base URL through their own validation.
    /// </summary>
    [TestMethod]
    public void Validate_WhenBaseUrlIsNull_ShouldThrowArgumentException()
    {
        EcbEndpointOptions endpoint = new() { BaseUrl = null! };

        _ = Assert.ThrowsExactly<ArgumentException>(() => endpoint.Validate());
    }
}
