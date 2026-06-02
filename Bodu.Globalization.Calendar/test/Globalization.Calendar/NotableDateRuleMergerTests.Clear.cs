// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleMergerTests.Clear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that a <c>&lt;Use&gt;</c> directive can explicitly clear inherited nullable fields, distinguishing "clear"
/// from "inherit" and "override" (assessment §6.4).
/// </summary>
[TestClass]
public sealed class NotableDateRuleMergerClearTests
{
    /// <summary>
    /// Verifies that clearing <c>territory</c> resets the inherited territory to <see langword="null" /> rather than
    /// preserving it.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveClearsTerritory_ShouldResetTerritoryToNull()
    {
        NotableDateRule source = SourceRule() with { TerritoryCode = "AU" };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            ClearFields: ClearSet("territory"));

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.IsNull(merged.TerritoryCode);
    }

    /// <summary>
    /// Verifies that clearing <c>comment</c>, <c>firstYear</c>, and <c>nonWorking</c> resets each inherited nullable
    /// field to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveClearsMultipleFields_ShouldResetEachToNull()
    {
        NotableDateRule source = SourceRule() with { Comment = "inherited", FirstYear = 1900, IsNonWorkingDay = true };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            ClearFields: ClearSet("comment", "firstYear", "nonWorking"));

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.IsNull(merged.Comment);
        Assert.IsNull(merged.FirstYear);
        Assert.IsNull(merged.IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that an explicit override value wins over a clear directive: clearing affects only the inheritance
    /// fallback, not an explicitly supplied value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenFieldClearedButBodySuppliesValue_ShouldUseSuppliedValue()
    {
        NotableDateRule source = SourceRule() with { TerritoryCode = "AU" };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            ClearFields: ClearSet("territory"),
            OverrideBody: new NotableDateRuleOverrideBody { TerritoryCode = "NZ" });

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual("NZ", merged.TerritoryCode);
    }

    /// <summary>
    /// Verifies that a field not named in the clear set is still inherited from the source.
    /// </summary>
    [TestMethod]
    public void Apply_WhenFieldNotCleared_ShouldInheritFromSource()
    {
        NotableDateRule source = SourceRule() with { TerritoryCode = "AU", Comment = "inherited" };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            ClearFields: ClearSet("comment"));

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual("AU", merged.TerritoryCode);
        Assert.IsNull(merged.Comment);
    }

    private static IReadOnlySet<string> ClearSet(params string[] fields) =>
        new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);

    private static NotableDateRule SourceRule() =>
        new()
        {
            Name = "Test",
            RuleName = "variant",
            Category = NotableDateCategory.Holiday,
            Strategy = DateResolutionStrategy.Fixed,
            Month = 1,
            Day = 1,
        };
}
