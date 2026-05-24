// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FranceNotableDatesTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data.Europe.Tests;

/// <summary>
/// Verifies that the France rule catalogue shipped in the Europe data pack flattens correctly through the
/// <see cref="XmlResourceNotableDateRuleProvider" /> — including locally declared French names such as Fête du Travail and
/// Bastille Day, and the absence of un-cherry-picked international observances.
/// </summary>
[TestClass]
public sealed class FranceNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the FR resource exposes the locally declared "Fête du Travail" while International Workers' Day is
    /// absent (because France did not cherry-pick it).
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenFrResource_ShouldExposeLocalNamesOnly()
    {
        var provider = EuropeCalendarData.CreateFranceProvider();

        var rules = provider.LoadRules().ToList();

        Assert.IsFalse(rules.Any(r => r.Name == "International Workers' Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Fête du Travail"));
        Assert.IsTrue(rules.Any(r => r.Name == "Bastille Day"));
    }
}
