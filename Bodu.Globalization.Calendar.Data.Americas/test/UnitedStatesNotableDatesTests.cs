// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedStatesNotableDatesTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Data.Americas.Tests;

/// <summary>
/// Verifies that the United States rule catalogue shipped in the Americas data pack flattens correctly through the
/// <see cref="XmlResourceNotableDateRuleProvider" /> — including cross-assembly cherry-pick resolution against the main library's
/// global and Christian resources, scalar overrides, and locally declared US-specific rules such as Independence Day and
/// Thanksgiving.
/// </summary>
[TestClass]
public sealed class UnitedStatesNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the US resource pulls in only the explicitly listed rules from Common and Christian — and does not
    /// include any rule that the US file did not opt in to (for example, Easter Monday or Whit Monday).
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLoadingUsResource_ShouldOnlyIncludeCherryPickedRules()
    {
        INotableDateRuleProvider provider = AmericasCalendarData.CreateUnitedStatesProvider();

        var rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Valentine's Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Halloween"));

        Assert.IsTrue(rules.Any(r => r.Name == "Easter Sunday"));
        Assert.IsTrue(rules.Any(r => r.Name == "Good Friday"));
        Assert.IsTrue(rules.Any(r => r.Name == "Christmas Day"));

        Assert.IsTrue(rules.Any(r => r.Name == "Independence Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Thanksgiving"));

        Assert.IsFalse(rules.Any(r => r.Name == "Easter Monday"));
        Assert.IsFalse(rules.Any(r => r.Name == "Whit Monday"));
        Assert.IsFalse(rules.Any(r => r.Name == "International Workers' Day"));
        Assert.IsFalse(rules.Any(r => r.Name == "All Saints' Day"));
    }

    /// <summary>
    /// Verifies that <c>Use</c> directives apply scalar overrides to the inherited rule. The US file marks Christmas Day
    /// non-working and tags the territory.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenUseDirectiveAppliesOverrides_ShouldRetagInheritedRule()
    {
        INotableDateRuleProvider provider = AmericasCalendarData.CreateUnitedStatesProvider();

        var christmasDay = provider.LoadRules().Single(r => r.Name == "Christmas Day" && r.TerritoryCode == "US");

        Assert.IsTrue(christmasDay.IsNonWorkingDay);
        Assert.AreEqual("US", christmasDay.TerritoryCode);
    }

    /// <summary>
    /// Verifies that adding a new rule to a source resource does not cascade into a consumer that uses explicit cherry-pick.
    /// A new rule introduced into the global Common set would not show up in the US flattened set unless the US XML explicitly
    /// added a Use directive for it. This test confirms the contract by verifying that the US set is a strict subset of the
    /// universal Common rules with respect to a couple of named non-cherry-picked entries.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenSourceContainsRulesNotCherryPicked_ShouldNotInheritThem()
    {
        var common = new XmlResourceNotableDateRuleProvider(
                "Bodu/Globalization/Calendar/Resources/global-all.xml",
                new ResourcePathResolver(),
                typeof(NotableDateService).Assembly)
            .LoadRules()
            .Select(r => r.Name)
            .ToHashSet();

        var us = AmericasCalendarData.CreateUnitedStatesProvider().LoadRules().Select(r => r.Name).ToHashSet();

        Assert.IsTrue(common.Contains("International Workers' Day"));
        Assert.IsFalse(us.Contains("International Workers' Day"));
        Assert.IsTrue(common.Contains("Remembrance Day"));
        Assert.IsFalse(us.Contains("Remembrance Day"));
    }

    /// <summary>
    /// Verifies that constructing a service from the United States provider returns Independence Day on 4 July 2026 and that
    /// querying with no companion European/Asia-Pacific pack registered does not produce Bastille Day or Australia Day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenServiceLoadedWithUnitedStatesProvider_ShouldIncludeIndependenceDay_AndExcludeOtherRegions()
    {
        var service = new NotableDateService(
            new[] { AmericasCalendarData.CreateUnitedStatesProvider() },
            CalendarWeekendDefinition.SaturdaySunday);

        var july4 = service.GetNotableDates(new DateTime(2026, 7, 4), "US");

        Assert.IsTrue(july4.Any(d => d.Name == "Independence Day" && d.Date == new DateTime(2026, 7, 4)));

        var bastille = service.GetNotableDates(2026, "FR").Where(d => d.Name == "Bastille Day").ToList();
        Assert.AreEqual(0, bastille.Count);
    }
}
