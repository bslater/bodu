// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeExchangeRateOptionsTests.AllowSynchronousNetworkAccess.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class XeExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the default leaves synchronous network access disabled.
    /// </summary>
    [TestMethod]
    public void AllowSynchronousNetworkAccess_WhenDefault_ShouldBeFalse()
    {
        XeExchangeRateOptions options = new();

        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
    }

    /// <summary>
    /// Verifies that the XE host is the default base address.
    /// </summary>
    [TestMethod]
    public void BaseAddress_WhenDefault_ShouldBeXeHost()
    {
        XeExchangeRateOptions options = new();

        Assert.AreEqual(new Uri("https://www.xe.com/"), options.BaseAddress);
    }
}
