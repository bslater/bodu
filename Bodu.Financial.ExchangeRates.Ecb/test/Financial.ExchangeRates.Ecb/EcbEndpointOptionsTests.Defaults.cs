// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbEndpointOptionsTests.Defaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

public partial class EcbEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the defaults target the ECB <c>eurofxref</c> path with a working timeout and user-agent.
    /// </summary>
    [TestMethod]
    public void Defaults_ShouldTargetEcbEurofxref()
    {
        EcbEndpointOptions endpoint = new();

        Assert.AreEqual(new Uri("https://www.ecb.europa.eu/stats/eurofxref/"), endpoint.BaseUrl);
        Assert.AreEqual(TimeSpan.FromSeconds(30), endpoint.HttpTimeout);
        Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.UserAgent));
    }
}
