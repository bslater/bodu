// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderOptionsTests.WebRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the options derive from <see cref="WebRateProviderOptions" />, so the endpoint, HTTP, look-back,
    /// refresh, alias and log-level settings are the shared members every web provider's options carry.
    /// </summary>
    [TestMethod]
    public void BaseType_ShouldBeWebRateProviderOptions()
    {
        Assert.AreEqual(typeof(WebRateProviderOptions), typeof(RbaRateProviderOptions).BaseType);
    }

    /// <summary>
    /// Verifies that the options declare only the RBA-specific members - the era catalog and the on-disk cache
    /// settings - and neither redeclare a shared member nor keep <c>BaseUrl</c> or
    /// <c>CurrentEraRefreshInterval</c>.
    /// </summary>
    [TestMethod]
    public void DeclaredProperties_ShouldBeOnlyTheRbaSpecificMembers()
    {
        string[] declared = typeof(RbaRateProviderOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "CacheDirectory", "EnableDiskCache", "Eras" }, declared);
    }
}
