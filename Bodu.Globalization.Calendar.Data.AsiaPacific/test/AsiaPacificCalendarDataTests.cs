// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsiaPacificCalendarDataTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies the public surface of <see cref="AsiaPacificCalendarData" /> — that
/// <see cref="AsiaPacificCalendarData.CreateProviders" /> yields one provider per supported Asia-Pacific country.
/// </summary>
[TestClass]
public sealed class AsiaPacificCalendarDataTests
{
    /// <summary>
    /// Verifies that <see cref="AsiaPacificCalendarData.CreateProviders" /> yields a provider for each of the eight supported
    /// Asia-Pacific countries.
    /// </summary>
    [TestMethod]
    public void CreateProviders_ShouldYieldOneProviderPerSupportedCountry()
    {
        var providers = AsiaPacificCalendarData.CreateProviders().ToList();

        Assert.AreEqual(8, providers.Count);
    }

    /// <summary>
    /// Verifies that <see cref="AsiaPacificCalendarData.DataAssembly" /> returns the pack assembly so consumers can build
    /// custom assembly chains.
    /// </summary>
    [TestMethod]
    public void DataAssembly_ShouldReturnTheCompanionPackAssembly()
    {
        Assert.AreEqual(typeof(AsiaPacificCalendarData).Assembly, AsiaPacificCalendarData.DataAssembly);
    }
}
