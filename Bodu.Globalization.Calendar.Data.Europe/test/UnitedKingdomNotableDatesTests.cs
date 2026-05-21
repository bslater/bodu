// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedKingdomNotableDatesTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data.Europe.Tests;

/// <summary>
/// Verifies that the United Kingdom rule catalogue shipped in the Europe data pack flattens correctly through the
/// <see cref="XmlResourceNotableDateRuleProvider" /> — including cross-assembly cherry-pick resolution against the main library's
/// global and Christian resources, locally declared overrides for shared rules such as New Year's Day, and UK-specific rules
/// such as Easter Monday, Boxing Day, and Burns Night.
/// </summary>
[TestClass]
public sealed class UnitedKingdomNotableDatesTests
{
    /// <summary>
    /// Verifies that loading the GB resource exposes Easter Monday (which the UK observes) because the GB XML explicitly
    /// cherry-picks it, independent of what other country files do.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLoadingGbResource_ShouldIncludeEasterMonday()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateUnitedKingdomProvider();

        var rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Any(r => r.Name == "Easter Monday"));
        Assert.IsTrue(rules.Any(r => r.Name == "Boxing Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Burns Night"));
    }

    /// <summary>
    /// Verifies that locally declared rules in a country file override the inherited rule with the same (name, territory) key.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLocalRuleOverridesInheritedRule_ShouldUseLocalRule()
    {
        INotableDateRuleProvider provider = EuropeCalendarData.CreateUnitedKingdomProvider();

        // New Year's Day is declared locally in the GB resource with a Tag and territory; the inherited Common rule should be
        // overridden.
        var newYears = provider.LoadRules().Single(r => r.Name == "New Year's Day");

        Assert.AreEqual("GB", newYears.TerritoryCode);
        Assert.IsTrue(newYears.Tags.Contains("BankHoliday"));
    }

    /// <summary>
    /// Verifies that Christmas Day for the UK on 25 December 2026 is returned by the service when configured with the UK
    /// provider.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenServiceLoadedWithUnitedKingdomProvider_ShouldIncludeChristmasDay_ForYear2026()
    {
        var service = new NotableDateService(
            new[] { EuropeCalendarData.CreateUnitedKingdomProvider() },
            WorkingDaysOfWeek.MondayToFriday);

        var christmas = service.GetNotableDates(new DateTime(2026, 12, 25), "GB");

        Assert.IsTrue(christmas.Any(d => d.Name == "Christmas Day" && d.Date == new DateTime(2026, 12, 25)));
    }
}
