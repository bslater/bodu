// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleMergerTests.RelativeWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleMerger" /> correctly switches a rule's strategy to and from
/// <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" /> via a <c>&lt;Use&gt;</c> override body, populating the
/// anchor and target fields and clearing the fields belonging to the other strategies.
/// </summary>
[TestClass]
public sealed class NotableDateRuleMergerRelativeWeekdayInMonthTests
{
    /// <summary>
    /// Verifies that overriding a fixed source rule with a <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" />
    /// body adopts the new strategy and fields and clears the inherited fixed-strategy day.
    /// </summary>
    [TestMethod]
    public void Apply_WhenOverrideSwitchesToRelativeWeekdayInMonth_ShouldPopulateFieldsAndClearOthers()
    {
        NotableDateRule source = new()
        {
            Name = "Sample",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
        };

        NotableDateRuleUseDirective directive = new(
            SourceRuleName: source.Name,
            OverrideBody: new NotableDateRuleOverrideBody
            {
                Strategy = DateResolutionStrategy.RelativeWeekdayInMonth,
                Month = 11,
                DayOfWeek = DayOfWeek.Monday,
                WeekOrdinal = WeekOfMonthOrdinal.First,
                RelativeDayOfWeek = DayOfWeek.Tuesday,
                WeekdayProximity = WeekdayProximity.OnOrAfter,
            });

        NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

        Assert.AreEqual(DateResolutionStrategy.RelativeWeekdayInMonth, merged.Strategy);
        Assert.AreEqual(11, merged.Month);
        Assert.AreEqual(DayOfWeek.Monday, merged.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.First, merged.WeekOrdinal);
        Assert.AreEqual(DayOfWeek.Tuesday, merged.RelativeDayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, merged.WeekdayProximity);
        Assert.IsNull(merged.Day);
    }

    /// <summary>
    /// Verifies that overriding a <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" /> source rule with a Fixed
    /// body adopts the Fixed strategy and clears the inherited <see cref="NotableDateRule.RelativeDayOfWeek" />,
    /// <see cref="NotableDateRule.WeekOrdinal" />, and <see cref="NotableDateRule.WeekdayProximity" /> fields.
    /// </summary>
    [TestMethod]
    public void Apply_WhenOverrideSwitchesAwayFromRelativeWeekdayInMonth_ShouldClearRelativeFields()
    {
        NotableDateRule source = new()
        {
            Name = "Sample",
            Strategy = DateResolutionStrategy.RelativeWeekdayInMonth,
            Category = NotableDateCategory.Civic,
            Month = 11,
            DayOfWeek = DayOfWeek.Monday,
            WeekOrdinal = WeekOfMonthOrdinal.First,
            RelativeDayOfWeek = DayOfWeek.Tuesday,
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
        Assert.IsNull(merged.RelativeDayOfWeek);
        Assert.IsNull(merged.WeekOrdinal);
        Assert.IsNull(merged.WeekdayProximity);
    }
}
