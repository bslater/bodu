// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeEndpointOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the options reject a missing base URL through their own validation.
    /// </summary>
    [TestMethod]
    public void Validate_WhenBaseUrlIsNull_ShouldThrowArgumentException()
    {
        BoeEndpointOptions endpoint = new() { BaseUrl = null! };

        _ = Assert.ThrowsExactly<ArgumentException>(endpoint.Validate);
    }

    /// <summary>
    /// Verifies that the options reject an empty query path through their own validation.
    /// </summary>
    [TestMethod]
    public void Validate_WhenQueryPathIsBlank_ShouldThrowArgumentException()
    {
        BoeEndpointOptions endpoint = new() { QueryPath = "   " };

        _ = Assert.ThrowsExactly<ArgumentException>(endpoint.Validate);
    }
}
