// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderOptionsTests.WebRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the options derive from <see cref="WebRateProviderOptions" />, so the endpoint, HTTP, look-back,
    /// refresh, alias and log-level settings are the shared members every web provider's options carry.
    /// </summary>
    [TestMethod]
    public void BaseType_ShouldBeWebRateProviderOptions()
    {
        Assert.AreEqual(typeof(WebRateProviderOptions), typeof(EcbRateProviderOptions).BaseType);
    }

    /// <summary>
    /// Verifies that the options declare only the ECB-specific members - the feed catalog and the on-disk cache
    /// settings - and neither redeclare a shared member nor keep the nested <c>Endpoint</c>.
    /// </summary>
    [TestMethod]
    public void DeclaredProperties_ShouldBeOnlyTheEcbSpecificMembers()
    {
        string[] declared = typeof(EcbRateProviderOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "CacheDirectory", "EnableDiskCache", "Feeds" }, declared);
    }

    /// <summary>
    /// Verifies that the package no longer ships the nested <c>EcbEndpointOptions</c> type, whose settings the shared
    /// <see cref="WebRateProviderOptions" /> members replace.
    /// </summary>
    [TestMethod]
    public void Assembly_ShouldNotDeclareEcbEndpointOptions()
    {
        Assert.IsNull(typeof(EcbRateProviderOptions).Assembly.GetType("Bodu.Financial.ExchangeRates.EcbEndpointOptions"));
    }
}
