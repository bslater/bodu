// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderOptionsTests.AllowSynchronousNetworkAccess.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the default leaves synchronous network access disabled.
    /// </summary>
    [TestMethod]
    public void AllowSynchronousNetworkAccess_WhenDefault_ShouldBeFalse()
    {
        RbaRateProviderOptions options = new();

        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
    }
}
