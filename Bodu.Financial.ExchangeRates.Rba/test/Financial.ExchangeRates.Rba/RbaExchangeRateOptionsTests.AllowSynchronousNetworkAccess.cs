// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateOptionsTests.AllowSynchronousNetworkAccess.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class RbaExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the default leaves synchronous network access disabled.
    /// </summary>
    [TestMethod]
    public void AllowSynchronousNetworkAccess_WhenDefault_ShouldBeFalse()
    {
        RbaExchangeRateOptions options = new();

        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
    }
}
