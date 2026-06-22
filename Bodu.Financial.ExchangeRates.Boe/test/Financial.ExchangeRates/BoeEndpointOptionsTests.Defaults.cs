// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeEndpointOptionsTests.Defaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the defaults target the Bank of England IADB path with a working timeout and user-agent.
    /// </summary>
    [TestMethod]
    public void Defaults_ShouldTargetIadb()
    {
        BoeEndpointOptions endpoint = new();

        Assert.AreEqual(new Uri("https://www.bankofengland.co.uk/boeapps/database/"), endpoint.BaseUrl);
        Assert.AreEqual("_iadb-fromshowcolumns.asp", endpoint.QueryPath);
        Assert.AreEqual(TimeSpan.FromSeconds(30), endpoint.HttpTimeout);
        Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.UserAgent));
    }
}
