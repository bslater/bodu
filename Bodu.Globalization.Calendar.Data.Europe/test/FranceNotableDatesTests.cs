// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FranceNotableDatesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies that the France rule catalogue shipped in the Europe data pack flattens correctly through
/// <see cref="XmlResourceNotableDateRuleProvider" /> — including locally declared French names such as
/// Fête du Travail and Bastille Day, and the absence of un-cherry-picked international observances such as
/// International Workers' Day.
/// </summary>
[TestClass]
public sealed class FranceNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the FR resource exposes the locally declared French names (Fête du Travail,
    /// Bastille Day) while International Workers' Day stays absent because France did not cherry-pick it.
    /// </summary>
    /// <param name="expectation">The catalogue expectation supplied by
    /// <see cref="FranceTerritoryAnswers.RuleCatalogueExpectations" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(FranceTerritoryAnswers.RuleCatalogueExpectations),
        typeof(FranceTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void LoadRules_WhenFrResource_ShouldRespectCherryPick(RuleCatalogueExpectation expectation) =>
        TerritoryNotableDateAssertions.AssertRuleCatalogue(expectation);
}
