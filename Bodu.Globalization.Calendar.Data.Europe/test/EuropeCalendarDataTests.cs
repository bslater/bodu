// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarDataTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar.Data.Europe.Tests;

/// <summary>
/// Verifies the public surface of <see cref="EuropeCalendarData" /> — that <see cref="EuropeCalendarData.CreateProviders" />
/// yields one provider per supported European country.
/// </summary>
[TestClass]
public sealed class EuropeCalendarDataTests
{
    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateProviders" /> yields a provider for each of the eight supported
    /// European countries.
    /// </summary>
    [TestMethod]
    public void CreateProviders_ShouldYieldOneProviderPerSupportedCountry()
    {
        var providers = EuropeCalendarData.CreateProviders().ToList();

        Assert.AreEqual(8, providers.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.DataAssembly" /> returns the pack assembly so consumers can build custom
    /// assembly chains.
    /// </summary>
    [TestMethod]
    public void DataAssembly_ShouldReturnTheCompanionPackAssembly()
    {
        Assert.AreEqual(typeof(EuropeCalendarData).Assembly, EuropeCalendarData.DataAssembly);
    }
}
