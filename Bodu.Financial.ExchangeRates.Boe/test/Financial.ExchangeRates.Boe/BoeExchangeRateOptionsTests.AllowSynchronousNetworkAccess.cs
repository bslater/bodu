// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateOptionsTests.AllowSynchronousNetworkAccess.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

public partial class BoeExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the default leaves synchronous network access disabled.
    /// </summary>
    [TestMethod]
    public void AllowSynchronousNetworkAccess_WhenDefault_ShouldBeFalse()
    {
        BoeExchangeRateOptions options = new();

        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
    }
}
