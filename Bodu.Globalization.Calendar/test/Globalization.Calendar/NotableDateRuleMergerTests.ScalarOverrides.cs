// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleMergerTests.ScalarOverrides.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the scalar-field merge behaviour of <see cref="NotableDateRuleMerger" /> across every authored
/// field on <see cref="NotableDateRuleUseDirective" /> and <see cref="NotableDateRuleOverrideBody" />.
/// Each test exercises one logical pathway: body wins over directive, directive wins over inherited, and
/// inherited preserved when both overrides are absent.
/// </summary>
[TestClass]
public sealed class NotableDateRuleMergerScalarOverridesTests
{
    /// <summary>
    /// Verifies that a flat <c>category</c> attribute on the directive overrides the inherited category.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOverridesCategory_ShouldUseDirectiveCategory()
    {
        NotableDateRule source = SourceRule() with { Category = NotableDateCategory.Religious };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            Category: NotableDateCategory.Civic);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(NotableDateCategory.Civic, merged.Category);
    }

    /// <summary>
    /// Verifies that an override body's <c>category</c> wins over the flat directive's <c>category</c>.
    /// </summary>
    [TestMethod]
    public void Apply_WhenBodyAndDirectiveDifferOnCategory_ShouldUseBody()
    {
        NotableDateRule source = SourceRule() with { Category = NotableDateCategory.Religious };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            Category: NotableDateCategory.Civic,
            OverrideBody: new NotableDateRuleOverrideBody { Category = NotableDateCategory.Cultural });

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(NotableDateCategory.Cultural, merged.Category);
    }

    /// <summary>
    /// Verifies that a flat <c>firstYear</c> attribute on the directive overrides the inherited value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOverridesFirstYear_ShouldUseDirectiveValue()
    {
        NotableDateRule source = SourceRule() with { FirstYear = 1900 };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            FirstYear: 2000);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(2000, merged.FirstYear);
    }

    /// <summary>
    /// Verifies that a flat <c>lastYear</c> attribute on the directive overrides the inherited value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOverridesLastYear_ShouldUseDirectiveValue()
    {
        NotableDateRule source = SourceRule() with { LastYear = 2100 };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            LastYear: 2050);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(2050, merged.LastYear);
    }

    /// <summary>
    /// Verifies that a flat <c>occurrenceYears</c> attribute on the directive overrides the inherited value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOverridesOccurrenceYears_ShouldUseDirectiveValue()
    {
        NotableDateRule source = SourceRule() with { OccurrenceYears = 1 };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            OccurrenceYears: 4);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(4, merged.OccurrenceYears);
    }

    /// <summary>
    /// Verifies that a flat <c>durationDays</c> attribute on the directive overrides the inherited value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOverridesDurationDays_ShouldUseDirectiveValue()
    {
        NotableDateRule source = SourceRule() with { DurationDays = 1 };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            DurationDays: 8);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(8, merged.DurationDays);
    }

    /// <summary>
    /// Verifies that a flat <c>priority</c> attribute on the directive overrides the inherited value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOverridesPriority_ShouldUseDirectiveValue()
    {
        NotableDateRule source = SourceRule() with { Priority = 100 };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            Priority: 1);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(1, merged.Priority);
    }

    /// <summary>
    /// Verifies that a flat <c>comment</c> attribute on the directive overrides the inherited value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOverridesComment_ShouldUseDirectiveValue()
    {
        NotableDateRule source = SourceRule() with { Comment = "Inherited" };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            Comment: "Overridden");

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual("Overridden", merged.Comment);
    }

    /// <summary>
    /// Verifies that an override body's <c>calendarType</c> wins over the inherited value.
    /// </summary>
    [TestMethod]
    public void Apply_WhenBodyOverridesCalendarType_ShouldUseBodyValue()
    {
        NotableDateRule source = SourceRule() with { CalendarType = typeof(SysGlobal.GregorianCalendar) };
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            OverrideBody: new NotableDateRuleOverrideBody { CalendarType = typeof(SysGlobal.HebrewCalendar) });

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(typeof(SysGlobal.HebrewCalendar), merged.CalendarType);
    }

    /// <summary>
    /// Verifies that omitting every override field preserves the inherited rule's scalar values.
    /// </summary>
    [TestMethod]
    public void Apply_WhenDirectiveOmitsAllOverrides_ShouldPreserveInheritedScalars()
    {
        NotableDateRule source = SourceRule() with
        {
            Category = NotableDateCategory.Cultural,
            TerritoryCode = "AU",
            IsNonWorkingDay = true,
            FirstYear = 1980,
            LastYear = 2080,
            OccurrenceYears = 4,
            DurationDays = 7,
            Priority = 5,
            Comment = "Preserved",
            CalendarType = typeof(SysGlobal.GregorianCalendar),
        };
        NotableDateRuleUseDirective directive = new(SourceRuleName: source.Name);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(source.Category, merged.Category);
        Assert.AreEqual(source.TerritoryCode, merged.TerritoryCode);
        Assert.AreEqual(source.IsNonWorkingDay, merged.IsNonWorkingDay);
        Assert.AreEqual(source.FirstYear, merged.FirstYear);
        Assert.AreEqual(source.LastYear, merged.LastYear);
        Assert.AreEqual(source.OccurrenceYears, merged.OccurrenceYears);
        Assert.AreEqual(source.DurationDays, merged.DurationDays);
        Assert.AreEqual(source.Priority, merged.Priority);
        Assert.AreEqual(source.Comment, merged.Comment);
        Assert.AreEqual(source.CalendarType, merged.CalendarType);
    }

    /// <summary>
    /// Verifies that override-body fields win over equivalent flat-directive fields (innermost-wins) across the full
    /// scalar surface.
    /// </summary>
    [TestMethod]
    public void Apply_WhenBodyAndDirectiveBothSpecifyScalars_ShouldUseBody()
    {
        NotableDateRule source = SourceRule();
        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            Category: NotableDateCategory.Civic,
            IsNonWorkingDay: true,
            FirstYear: 1900,
            LastYear: 2000,
            OccurrenceYears: 5,
            DurationDays: 2,
            Priority: 7,
            Comment: "FromDirective",
            OverrideBody: new NotableDateRuleOverrideBody
            {
                Category = NotableDateCategory.Religious,
                IsNonWorkingDay = false,
                FirstYear = 1950,
                LastYear = 2050,
                OccurrenceYears = 1,
                DurationDays = 8,
                Priority = 1,
                Comment = "FromBody",
            });

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(NotableDateCategory.Religious, merged.Category);
        Assert.AreEqual(false, merged.IsNonWorkingDay);
        Assert.AreEqual(1950, merged.FirstYear);
        Assert.AreEqual(2050, merged.LastYear);
        Assert.AreEqual(1, merged.OccurrenceYears);
        Assert.AreEqual(8, merged.DurationDays);
        Assert.AreEqual(1, merged.Priority);
        Assert.AreEqual("FromBody", merged.Comment);
    }

    /// <summary>
    /// Verifies that a directive with no override body and no flat <c>territoryCode</c> preserves the inherited
    /// <see cref="NotableDateRule.TerritoryCode" />.
    /// </summary>
    [TestMethod]
    public void Apply_WhenNoTerritoryOverride_ShouldPreserveInheritedTerritory()
    {
        NotableDateRule source = SourceRule() with { TerritoryCode = "AU,NZ" };
        NotableDateRuleUseDirective directive = new(SourceRuleName: source.Name);

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual("AU,NZ", merged.TerritoryCode);
    }

    private static NotableDateRule SourceRule() => new()
    {
        Name = "Inherited Holiday",
        Strategy = DateResolutionStrategy.Fixed,
        Category = NotableDateCategory.Holiday,
        Month = 1,
        Day = 1,
        Tags = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
        Adjustments = ImmutableArray<ObservanceAdjustment>.Empty,
    };
}
