// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarDataTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateGermanyProvider" /> returns a non-null rule provider whose rules
    /// can be enumerated.
    /// </summary>
    [TestMethod]
    public void CreateGermanyProvider_ShouldReturnUsableRuleProvider()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateGermanyProvider();

        Assert.IsNotNull(provider);
        Assert.IsTrue(provider.LoadRules().Any(), "Germany provider should yield at least one rule.");
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateSpainProvider" /> returns a non-null rule provider whose rules
    /// can be enumerated.
    /// </summary>
    [TestMethod]
    public void CreateSpainProvider_ShouldReturnUsableRuleProvider()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateSpainProvider();

        Assert.IsNotNull(provider);
        Assert.IsTrue(provider.LoadRules().Any(), "Spain provider should yield at least one rule.");
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateFranceProvider" /> returns a non-null rule provider whose rules
    /// can be enumerated.
    /// </summary>
    [TestMethod]
    public void CreateFranceProvider_ShouldReturnUsableRuleProvider()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateFranceProvider();

        Assert.IsNotNull(provider);
        Assert.IsTrue(provider.LoadRules().Any(), "France provider should yield at least one rule.");
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateIrelandProvider" /> returns a non-null rule provider whose rules
    /// can be enumerated.
    /// </summary>
    [TestMethod]
    public void CreateIrelandProvider_ShouldReturnUsableRuleProvider()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateIrelandProvider();

        Assert.IsNotNull(provider);
        Assert.IsTrue(provider.LoadRules().Any(), "Ireland provider should yield at least one rule.");
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateItalyProvider" /> returns a non-null rule provider whose rules
    /// can be enumerated.
    /// </summary>
    [TestMethod]
    public void CreateItalyProvider_ShouldReturnUsableRuleProvider()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateItalyProvider();

        Assert.IsNotNull(provider);
        Assert.IsTrue(provider.LoadRules().Any(), "Italy provider should yield at least one rule.");
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateNetherlandsProvider" /> returns a non-null rule provider whose
    /// rules can be enumerated.
    /// </summary>
    [TestMethod]
    public void CreateNetherlandsProvider_ShouldReturnUsableRuleProvider()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateNetherlandsProvider();

        Assert.IsNotNull(provider);
        Assert.IsTrue(provider.LoadRules().Any(), "Netherlands provider should yield at least one rule.");
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateSwedenProvider" /> returns a non-null rule provider whose rules
    /// can be enumerated.
    /// </summary>
    [TestMethod]
    public void CreateSwedenProvider_ShouldReturnUsableRuleProvider()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateSwedenProvider();

        Assert.IsNotNull(provider);
        Assert.IsTrue(provider.LoadRules().Any(), "Sweden provider should yield at least one rule.");
    }

    /// <summary>
    /// Verifies that <see cref="EuropeCalendarData.CreateProvider" /> rejects a <see langword="null" /> resource name with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateProvider_WhenResourceNameIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EuropeCalendarData.CreateProvider(null!);
        });
    }

    /// <summary>
    /// Verifies that every provider yielded by <see cref="EuropeCalendarData.CreateProviders" /> exposes at least one rule.
    /// </summary>
    [TestMethod]
    public void CreateProviders_EveryProviderShouldYieldAtLeastOneRule()
    {
        foreach (INotableDateRuleProvider provider in EuropeCalendarData.CreateProviders())
        {
            Assert.IsTrue(provider.LoadRules().Any(), "Every Europe provider should yield at least one rule.");
        }
    }
}
