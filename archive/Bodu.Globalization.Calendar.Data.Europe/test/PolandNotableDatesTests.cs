// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolandNotableDatesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies that the Poland rule catalogue shipped in the Europe data pack flattens correctly through
/// <see cref="XmlResourceNotableDateRuleProvider" /> — including dates inherited from the shared
/// <c>europe-common.xml</c> file (Easter Monday, Corpus Christi, Christmas Day), Poland-specific national
/// holidays, and the deliberate absence of un-cherry-picked dates such as Good Friday.
/// </summary>
[TestClass]
public sealed class PolandNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the PL resource exposes the dates inherited from <c>europe-common.xml</c> and the
    /// Poland-specific national holidays, while Good Friday (not cherry-picked) and the global "Boxing Day" name
    /// (locally renamed to "Second Day of Christmas") stay absent.
    /// </summary>
    /// <param name="expectation">The catalogue expectation supplied by
    /// <see cref="PolandTerritoryAnswers.RuleCatalogueExpectations" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(PolandTerritoryAnswers.RuleCatalogueExpectations),
        typeof(PolandTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void LoadRules_WhenPlResource_ShouldInheritCommonAndRespectCherryPick(RuleCatalogueExpectation expectation) =>
        TerritoryNotableDateAssertions.AssertRuleCatalogue(expectation);
}
