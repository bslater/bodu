// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbEndpointOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the endpoint defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        EcbEndpointOptions endpoint = new();

        bool valid = endpoint.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive endpoint HTTP timeout is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHttpTimeoutIsZero_ShouldReturnFalse()
    {
        EcbEndpointOptions endpoint = new() { HttpTimeout = TimeSpan.Zero };

        bool valid = endpoint.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
