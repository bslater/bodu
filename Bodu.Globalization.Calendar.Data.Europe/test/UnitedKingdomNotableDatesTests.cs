// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedKingdomNotableDatesTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies that the United Kingdom rule catalogue shipped in the Europe data pack flattens correctly
/// through <see cref="XmlResourceNotableDateRuleProvider" /> — including cross-assembly cherry-pick
/// resolution, locally declared overrides for shared rules such as New Year's Day, and UK-specific rules
/// such as Easter Monday, Boxing Day, and Burns Night.
/// </summary>
[TestClass]
public sealed class UnitedKingdomNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the GB resource exposes the cherry-picked rules — Easter Monday, Boxing Day,
    /// and Burns Night must appear in the flattened catalogue.
    /// </summary>
    /// <param name="expectation">The catalogue expectation supplied by
    /// <see cref="UnitedKingdomTerritoryAnswers.RuleCatalogueExpectations" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(UnitedKingdomTerritoryAnswers.RuleCatalogueExpectations),
        typeof(UnitedKingdomTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void LoadRules_WhenLoadingGbResource_ShouldRespectCherryPick(RuleCatalogueExpectation expectation) =>
        TerritoryNotableDateAssertions.AssertRuleCatalogue(expectation);

    /// <summary>
    /// Verifies that locally declared rules in the GB file override the inherited rule with the same
    /// (name, territory) key. New Year's Day is declared locally in the GB resource with a Tag and a
    /// territory — the inherited Common rule must be overridden. This is an override-payload assertion
    /// (asserting the override's TerritoryCode and Tags) and stays bespoke.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLocalRuleOverridesInheritedRule_ShouldUseLocalRule()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateUnitedKingdomProvider();

        NotableDateRule newYears = provider.LoadRules().Single(r => r.Name == "New Year's Day");

        Assert.AreEqual("GB", newYears.TerritoryCode);
        Assert.IsTrue(newYears.Tags.Contains("BankHoliday"));
    }
}
