// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleMergerTests.WeekdayNearDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleMerger" /> correctly switches a rule's strategy to and from
/// <see cref="DateResolutionStrategy.WeekdayNearDate" /> via a <c>&lt;Use&gt;</c> override body, populating the
/// WeekdayNearDate fields and clearing the fields belonging to the other strategies.
/// </summary>
[TestClass]
public sealed class NotableDateRuleMergerWeekdayNearDateTests
{
    /// <summary>
    /// Verifies that overriding an offset-anchored source rule with a <see cref="DateResolutionStrategy.WeekdayNearDate" />
    /// body adopts the new strategy and fields and clears the inherited anchor/offset fields.
    /// </summary>
    [TestMethod]
    public void Apply_WhenOverrideSwitchesToWeekdayNearDate_ShouldPopulateFieldsAndClearOthers()
    {
        NotableDateRule source = new()
        {
            Name = "Sample",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Easter Sunday",
            OffsetDays = 1,
        };

        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            OverrideBody: new NotableDateRuleOverrideBody
            {
                Strategy = DateResolutionStrategy.WeekdayNearDate,
                Month = 6,
                Day = 20,
                DayOfWeek = DayOfWeek.Saturday,
                WeekdayProximity = WeekdayProximity.OnOrAfter,
            });

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(DateResolutionStrategy.WeekdayNearDate, merged.Strategy);
        Assert.AreEqual(6, merged.Month);
        Assert.AreEqual(20, merged.Day);
        Assert.AreEqual(DayOfWeek.Saturday, merged.DayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, merged.WeekdayProximity);
        Assert.IsNull(merged.AnchorRuleName);
        Assert.IsNull(merged.OffsetDays);
    }

    /// <summary>
    /// Verifies that overriding a <see cref="DateResolutionStrategy.WeekdayNearDate" /> source rule with a Fixed body
    /// adopts the Fixed strategy and clears the inherited <see cref="NotableDateRule.WeekdayProximity" /> and
    /// <see cref="NotableDateRule.DayOfWeek" /> fields.
    /// </summary>
    [TestMethod]
    public void Apply_WhenOverrideSwitchesAwayFromWeekdayNearDate_ShouldClearProximity()
    {
        NotableDateRule source = new()
        {
            Name = "Sample",
            Strategy = DateResolutionStrategy.WeekdayNearDate,
            Category = NotableDateCategory.Holiday,
            Month = 6,
            Day = 20,
            DayOfWeek = DayOfWeek.Saturday,
            WeekdayProximity = WeekdayProximity.OnOrAfter,
            Tags = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
        };

        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            OverrideBody: new NotableDateRuleOverrideBody
            {
                Strategy = DateResolutionStrategy.Fixed,
                Month = 12,
                Day = 25,
            });

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(DateResolutionStrategy.Fixed, merged.Strategy);
        Assert.AreEqual(12, merged.Month);
        Assert.AreEqual(25, merged.Day);
        Assert.IsNull(merged.WeekdayProximity);
        Assert.IsNull(merged.DayOfWeek);
    }
}
