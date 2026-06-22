// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateOptionsTests.AllowSynchronousNetworkAccess.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class YahooExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the default leaves synchronous network access disabled.
    /// </summary>
    [TestMethod]
    public void AllowSynchronousNetworkAccess_WhenDefault_ShouldBeFalse()
    {
        YahooExchangeRateOptions options = new();

        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
    }
}
