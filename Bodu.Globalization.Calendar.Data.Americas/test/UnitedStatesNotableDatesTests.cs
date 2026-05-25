// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedStatesNotableDatesTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Data.Americas.Tests;

/// <summary>
/// Verifies that the United States rule catalogue shipped in the Americas data pack flattens correctly
/// through <see cref="XmlResourceNotableDateRuleProvider" /> — including cross-assembly cherry-pick
/// resolution against the main library's global and Christian resources, scalar overrides, and locally
/// declared US-specific rules such as Independence Day and Thanksgiving.
/// </summary>
[TestClass]
public sealed class UnitedStatesNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the US resource flattens to exactly the cherry-picked rules — local and
    /// inherited inclusions must appear, deliberately omitted observances must not.
    /// </summary>
    /// <param name="expectation">The catalogue expectation supplied by
    /// <see cref="UnitedStatesTerritoryAnswers.RuleCatalogueExpectations" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(UnitedStatesTerritoryAnswers.RuleCatalogueExpectations),
        typeof(UnitedStatesTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void LoadRules_WhenLoadingUsResource_ShouldRespectCherryPick(RuleCatalogueExpectation expectation) =>
        TerritoryNotableDateAssertions.AssertRuleCatalogue(expectation);

    /// <summary>
    /// Verifies United States named-holiday occurrences via the consolidated KAT — currently Independence
    /// Day on 4 July 2026.
    /// </summary>
    /// <param name="knownAnswer">The known-answer row supplied by
    /// <see cref="UnitedStatesTerritoryAnswers.NamedHolidayOccurrences" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(UnitedStatesTerritoryAnswers.NamedHolidayOccurrences),
        typeof(UnitedStatesTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void GetNotableDates_WhenGivenKnownYearAndTerritory_ShouldReturnExpectedOccurrence(TerritoryNotableDateKnownAnswer knownAnswer) =>
        TerritoryNotableDateAssertions.AssertExpectedOccurrence(knownAnswer);

    /// <summary>
    /// Verifies that <c>Use</c> directives apply scalar overrides to inherited rules. The US file marks
    /// Christmas Day non-working and tags the territory — this is an override-payload assertion (not a
    /// presence / occurrence check) and stays bespoke.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenUseDirectiveAppliesOverrides_ShouldRetagInheritedRule()
    {
        INotableDateRuleProvider provider = AmericasCalendarData.CreateUnitedStatesProvider();

        NotableDateRule christmasDay = provider.LoadRules().Single(r => r.Name == "Christmas Day" && r.TerritoryCode == "US");

        Assert.IsTrue(christmasDay.IsNonWorkingDay);
        Assert.AreEqual("US", christmasDay.TerritoryCode);
    }

    /// <summary>
    /// Verifies that adding a new rule to a source resource does not cascade into a consumer that uses
    /// explicit cherry-pick. The contract is "the US set is a strict subset of the universal Common rules
    /// with respect to a couple of named non-cherry-picked entries". The assertion shape (loading a second
    /// provider to compute a complement set) does not fit the per-row KAT and stays bespoke.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenSourceContainsRulesNotCherryPicked_ShouldNotInheritThem()
    {
        HashSet<string> common = new XmlResourceNotableDateRuleProvider(
                "Bodu/Globalization/Calendar/Resources/global-all.xml",
                new ResourcePathResolver(),
                typeof(NotableDateService).Assembly)
            .LoadRules()
            .Select(r => r.Name)
            .ToHashSet();

        HashSet<string> us = AmericasCalendarData.CreateUnitedStatesProvider().LoadRules().Select(r => r.Name).ToHashSet();

        Assert.IsTrue(common.Contains("International Workers' Day"));
        Assert.IsFalse(us.Contains("International Workers' Day"));
        Assert.IsTrue(common.Contains("Remembrance Day"));
        Assert.IsFalse(us.Contains("Remembrance Day"));
    }

    /// <summary>
    /// Verifies that querying for Bastille Day on the US-only service yields zero results — confirms that
    /// loading the US provider does NOT transitively pull in European rules. This is a service-level
    /// cross-region exclusion check that complements the per-row Independence Day KAT.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenServiceLoadedWithUnitedStatesProvider_ShouldNotIncludeOtherRegions()
    {
        NotableDateService service = new(
            new[] { AmericasCalendarData.CreateUnitedStatesProvider() },
            WorkingDaysOfWeek.MondayToFriday);

        List<NotableDate> bastille = service.GetNotableDates(2026, "FR").Where(d => d.Name == "Bastille Day").ToList();

        Assert.AreEqual(0, bastille.Count);
    }
}
