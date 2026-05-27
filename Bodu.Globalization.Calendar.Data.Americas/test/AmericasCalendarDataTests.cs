// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmericasCalendarDataTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies the public surface of <see cref="AmericasCalendarData" /> — that each per-country factory produces a flattened rule
/// set whose root payload comes from the pack assembly, and that <see cref="AmericasCalendarData.CreateProviders" /> yields one
/// provider per supported country.
/// </summary>
[TestClass]
public sealed class AmericasCalendarDataTests
{
    /// <summary>
    /// Verifies that <see cref="AmericasCalendarData.CreateProviders" /> yields a provider for the United States and Canada in
    /// the documented order.
    /// </summary>
    [TestMethod]
    public void CreateProviders_ShouldEnumerateUnitedStatesThenCanada()
    {
        var providers = AmericasCalendarData.CreateProviders().ToList();

        Assert.AreEqual(2, providers.Count);
        Assert.IsTrue(providers[0].LoadRules().Any(r => r.Name == "Independence Day" && r.TerritoryCode == "US"));
        Assert.IsTrue(providers[1].LoadRules().Any(r => r.TerritoryCode == "CA"));
    }

    /// <summary>
    /// Verifies that <see cref="AmericasCalendarData.DataAssembly" /> returns the pack assembly so consumers can build custom
    /// assembly chains.
    /// </summary>
    [TestMethod]
    public void DataAssembly_ShouldReturnTheCompanionPackAssembly()
    {
        Assert.AreEqual(typeof(AmericasCalendarData).Assembly, AmericasCalendarData.DataAssembly);
    }

    /// <summary>
    /// Verifies that <see cref="AmericasCalendarData.CreateProvider(string)" /> rejects a <see langword="null" /> resource
    /// name with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateProvider_WhenResourceNameIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = AmericasCalendarData.CreateProvider(null!);
        });

        Assert.AreEqual("rootResourceName", ex.ParamName);
    }
}
