// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderOptionsTests.WebRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the options derive from <see cref="WebRateProviderOptions" />, so the endpoint, HTTP, look-back,
    /// refresh, alias and log-level settings are the shared members every web provider's options carry.
    /// </summary>
    [TestMethod]
    public void BaseType_ShouldBeWebRateProviderOptions()
    {
        Assert.AreEqual(typeof(WebRateProviderOptions), typeof(BoeRateProviderOptions).BaseType);
    }

    /// <summary>
    /// Verifies that the options declare only the Bank of England-specific members - the series catalog, the IADB
    /// query path and the on-disk cache settings - and neither redeclare a shared member nor keep the nested
    /// <c>Endpoint</c> or the <c>OnDemandWindowDays</c> count.
    /// </summary>
    [TestMethod]
    public void DeclaredProperties_ShouldBeOnlyTheBankOfEnglandSpecificMembers()
    {
        string[] declared = typeof(BoeRateProviderOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "CacheDirectory", "EnableDiskCache", "QueryPath", "Series" }, declared);
    }

    /// <summary>
    /// Verifies that the package no longer ships the nested <c>BoeEndpointOptions</c> type, whose settings the shared
    /// <see cref="WebRateProviderOptions" /> members and <c>QueryPath</c> replace.
    /// </summary>
    [TestMethod]
    public void Assembly_ShouldNotDeclareBoeEndpointOptions()
    {
        Assert.IsNull(typeof(BoeRateProviderOptions).Assembly.GetType("Bodu.Financial.ExchangeRates.BoeEndpointOptions"));
    }
}
