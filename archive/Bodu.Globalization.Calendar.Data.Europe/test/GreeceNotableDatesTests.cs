// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GreeceNotableDatesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies that the Greece rule catalogue shipped in the Europe data pack flattens correctly through
/// <see cref="XmlResourceNotableDateRuleProvider" /> — combining fixed Gregorian dates inherited from the shared
/// <c>europe-common.xml</c> file with the movable Eastern Orthodox Easter cycle pulled from the main library, and
/// confirming the Western Easter dates are absent because Greece follows the Orthodox calendar.
/// </summary>
[TestClass]
public sealed class GreeceNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the GR resource exposes the inherited Gregorian commons and the Orthodox Easter cycle,
    /// while the Western "Good Friday" / "Easter Monday" names and the un-renamed "Assumption of Mary" stay absent.
    /// </summary>
    /// <param name="expectation">The catalogue expectation supplied by
    /// <see cref="GreeceTerritoryAnswers.RuleCatalogueExpectations" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(GreeceTerritoryAnswers.RuleCatalogueExpectations),
        typeof(GreeceTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void LoadRules_WhenGrResource_ShouldCombineCommonAndOrthodoxCycle(RuleCatalogueExpectation expectation) =>
        TerritoryNotableDateAssertions.AssertRuleCatalogue(expectation);
}
