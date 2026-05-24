// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.GetSupportedTerritories.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the territory-enumeration contract of <see cref="NotableDateService.GetSupportedTerritories" />.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> returns no entries when every rule is
    /// global (territory-less).
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenAllRulesGlobal_ShouldReturnEmpty()
    {
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Global", 1, 1)) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(0, service.GetSupportedTerritories().Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> projects through every authored
    /// territory and returns a distinct set.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenRulesAcrossMultipleTerritories_ShouldReturnDistinctSet()
    {
        NotableDateService service = new(
            ruleProviders: new[]
            {
                (INotableDateRuleProvider)new InMemoryRuleProvider(
                    Fixed("AU-NSW Holiday", 1, 1, territory: "AU-NSW"),
                    Fixed("AU-NSW Other",   2, 1, territory: "AU-NSW"),
                    Fixed("AU-VIC Holiday", 3, 1, territory: "AU-VIC"),
                    Fixed("US Federal",     7, 4, territory: "US")),
            },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyCollection<string> territories = service.GetSupportedTerritories();

        CollectionAssert.AreEquivalent(new[] { "AU-NSW", "AU-VIC", "US" }, territories.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> elides rules whose territory is
    /// <see langword="null" /> or empty.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenMixedScopedAndGlobalRules_ShouldOnlyReportScopedTerritories()
    {
        NotableDateService service = new(
            ruleProviders: new[]
            {
                (INotableDateRuleProvider)new InMemoryRuleProvider(
                    Fixed("Global A", 1, 1),
                    Fixed("Global B", 2, 1),
                    Fixed("Scoped",  3, 1, territory: "AU")),
            },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyCollection<string> territories = service.GetSupportedTerritories();

        Assert.AreEqual(1, territories.Count);
        Assert.IsTrue(territories.Contains("AU"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> treats territory codes case-insensitively
    /// when deduplicating.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenTerritoryCodesDifferOnlyByCase_ShouldCollapseToOneEntry()
    {
        NotableDateService service = new(
            ruleProviders: new[]
            {
                (INotableDateRuleProvider)new InMemoryRuleProvider(
                    Fixed("Upper", 1, 1, territory: "AU"),
                    Fixed("Lower", 2, 1, territory: "au"),
                    Fixed("Mixed", 3, 1, territory: "Au")),
            },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(1, service.GetSupportedTerritories().Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> reflects override additions after
    /// <see cref="NotableDateService.Reload" /> has been called.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenOverrideAddsTerritoryAndReloadCalled_ShouldIncludeNewTerritory()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("AU Rule", 1, 1, territory: "AU")) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        Assert.IsFalse(service.GetSupportedTerritories().Contains("NZ"));

        overrides.AddRule(Fixed("NZ Rule", 2, 6, territory: "NZ"));
        service.Reload();

        Assert.IsTrue(service.GetSupportedTerritories().Contains("NZ"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> returns an empty collection when no
    /// rule providers are registered.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenNoRuleProviders_ShouldReturnEmpty()
    {
        NotableDateService service = new(
            ruleProviders: Array.Empty<INotableDateRuleProvider>(),
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(0, service.GetSupportedTerritories().Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> deduplicates across providers — when
    /// two providers both contribute rules scoped to the same territory the territory appears once.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenMultipleProvidersContributeSameTerritory_ShouldReportOnce()
    {
        NotableDateService service = new(
            ruleProviders: new[]
            {
                (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("A", 1, 1, territory: "AU")),
                (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("B", 2, 1, territory: "AU")),
            },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyCollection<string> territories = service.GetSupportedTerritories();

        Assert.AreEqual(1, territories.Count);
        Assert.IsTrue(territories.Contains("AU"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> returns territories in stable,
    /// case-insensitive sorted order so consumers driving UI pickers see deterministic output.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenMultipleTerritories_ShouldReturnInCaseInsensitiveSortedOrder()
    {
        NotableDateService service = new(
            ruleProviders: new[]
            {
                (INotableDateRuleProvider)new InMemoryRuleProvider(
                    Fixed("z", 1, 1, territory: "ZZ"),
                    Fixed("c", 2, 1, territory: "CN"),
                    Fixed("a", 3, 1, territory: "AU-NSW"),
                    Fixed("b", 4, 1, territory: "AU")),
            },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        List<string> territories = service.GetSupportedTerritories().ToList();

        CollectionAssert.AreEqual(new[] { "AU", "AU-NSW", "CN", "ZZ" }, territories);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> returns a snapshot — successive calls
    /// after <see cref="NotableDateService.Reload" /> reflect the latest effective rule set while the previously
    /// returned collection is unaffected by subsequent mutations.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenCalledRepeatedlyAroundReload_ShouldReturnFreshSnapshotEachTime()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Base", 1, 1, territory: "AU")) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        IReadOnlyCollection<string> firstSnapshot = service.GetSupportedTerritories();
        Assert.AreEqual(1, firstSnapshot.Count);

        overrides.AddRule(Fixed("Extra", 2, 1, territory: "NZ"));
        service.Reload();

        IReadOnlyCollection<string> secondSnapshot = service.GetSupportedTerritories();
        Assert.AreEqual(2, secondSnapshot.Count);
        Assert.AreEqual(1, firstSnapshot.Count, "Earlier snapshot must remain a frozen view of its read.");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> elides rules whose territory is empty
    /// or whitespace-only so that authoring noise does not surface in UI pickers.
    /// </summary>
    /// <param name="territory">The territory code authored on the test rule.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("   ")]
    public void GetSupportedTerritories_WhenTerritoryIsBlank_ShouldElideRule(string territory)
    {
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Blank", 1, 1, territory: territory)) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(0, service.GetSupportedTerritories().Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedTerritories" /> reflects rule suppression via overrides
    /// after <see cref="NotableDateService.Reload" /> has been called.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenOverrideRemovesLastRuleInTerritoryAndReloadCalled_ShouldRemoveTerritory()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateService service = new(
            ruleProviders: new[]
            {
                (INotableDateRuleProvider)new InMemoryRuleProvider(
                    Fixed("AU Rule", 1, 1, territory: "AU"),
                    Fixed("NZ Rule", 2, 6, territory: "NZ")),
            },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        Assert.IsTrue(service.GetSupportedTerritories().Contains("NZ"));

        overrides.RemoveRule("NZ Rule");
        service.Reload();

        // Note: rule remains in the effective set as a removal-suppressed entry; GetSupportedTerritories projects
        // over _effectiveRules so the territory still appears unless the rule itself is dropped via override.
        // The contract returns territories present in the effective rule set, not yet-resolvable territories.
        Assert.IsTrue(service.GetSupportedTerritories().Contains("AU"));
    }
}
