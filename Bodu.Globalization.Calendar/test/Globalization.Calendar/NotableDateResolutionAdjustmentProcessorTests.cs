// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionAdjustmentProcessorTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="NotableDateResolutionAdjustmentProcessor" /> class.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionAdjustmentProcessorTests
{
    /// <summary>
    /// Verifies that construction rejects an undefined weekend definition.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenWeekendDefinitionIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new NotableDateResolutionAdjustmentProcessor((CalendarWeekendDefinition)999);
        });

        Assert.AreEqual("weekendDefinition", ex.ParamName);
    }

    /// <summary>
    /// Verifies that applying adjustments rejects a null window.
    /// </summary>
    [TestMethod]
    public void ApplyAdjustments_WhenWindowIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            processor.ApplyAdjustments(null!, Array.Empty<ResolvedNotableDateOccurrence>());
        });

        Assert.AreEqual("window", ex.ParamName);
    }

    /// <summary>
    /// Verifies that applying adjustments rejects a null occurrence list.
    /// </summary>
    [TestMethod]
    public void ApplyAdjustments_WhenOccurrencesIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
        NotableDateResolutionWindow window = new(new DateTime(2021, 12, 1), new DateTime(2021, 12, 31));

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            processor.ApplyAdjustments(window, null!);
        });

        Assert.AreEqual("occurrences", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adjacent weekend public holidays allocate distinct substitute weekdays.
    /// </summary>
    [TestMethod]
    public void ApplyAdjustments_WhenAdjacentWeekendHolidaysShiftForward_ShouldAllocateDistinctSubstituteDays()
    {
        NotableDateRule christmasRule = FixedPublicHolidayRule("Christmas Day", month: 12, day: 25);
        NotableDateRule boxingDayRule = FixedPublicHolidayRule("Boxing Day", month: 12, day: 26);

        ResolvedNotableDateOccurrence christmas = Occurrence(christmasRule, new DateTime(2021, 12, 25));
        ResolvedNotableDateOccurrence boxingDay = Occurrence(boxingDayRule, new DateTime(2021, 12, 26));

        NotableDateResolutionWindow window = new(new DateTime(2021, 12, 24), new DateTime(2021, 12, 31));
        window.AddBase(christmas);
        window.AddBase(boxingDay);

        NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);

        processor.ApplyAdjustments(window, [christmas, boxingDay]);

        IReadOnlyList<NotableDate> output = window.OutputDates;

        Assert.IsTrue(output.Any(date =>
            date.Name == "Christmas Day" &&
            date.Date == new DateTime(2021, 12, 27) &&
            date.WasAdjusted));

        Assert.IsTrue(output.Any(date =>
            date.Name == "Boxing Day" &&
            date.Date == new DateTime(2021, 12, 28) &&
            date.WasAdjusted));

        Assert.IsFalse(output.Any(date =>
            date.Name == "Boxing Day" &&
            date.Date == new DateTime(2021, 12, 27) &&
            date.WasAdjusted));
    }

    /// <summary>
    /// Verifies that adjustment metadata is attached to the emitted adjusted occurrence.
    /// </summary>
    [TestMethod]
    public void ApplyAdjustments_WhenAdjustmentActivates_ShouldAttachAdjustmentReason()
    {
        NotableDateRule rule = FixedPublicHolidayRule("Christmas Day", month: 12, day: 25);
        ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2021, 12, 25));

        NotableDateResolutionWindow window = new(new DateTime(2021, 12, 24), new DateTime(2021, 12, 31));
        window.AddBase(occurrence);

        NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);

        processor.ApplyAdjustments(window, [occurrence]);

        NotableDate adjusted = window.OutputDates.Single(date => date.WasAdjusted);

        Assert.AreEqual(new DateTime(2021, 12, 27), adjusted.Date);
        Assert.IsNotNull(adjusted.AdjustmentReason);
        Assert.AreEqual(new DateTime(2021, 12, 25), adjusted.AdjustmentReason.OriginalDate);
        Assert.AreEqual(AdjustmentTrigger.IfWeekend, adjusted.AdjustmentReason.Trigger);
        Assert.AreEqual(AdjustmentAction.MoveToNextNonWorkingDay, adjusted.AdjustmentReason.Action);
        Assert.IsTrue(adjusted.IsNonWorkingDay);
    }

    private static NotableDateRule FixedPublicHolidayRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(
                new ObservanceAdjustment
                {
                    Key = "weekend-substitute",
                    Trigger = AdjustmentTrigger.IfWeekend,
                    Action = AdjustmentAction.MoveToNextNonWorkingDay,
                    IsNonWorkingDay = true,
                }),
        };

    private static ResolvedNotableDateOccurrence Occurrence(NotableDateRule rule, DateTime date)
    {
        NotableDate notable = new()
        {
            Name = rule.Name,
            Date = date.Date,
            Category = rule.Category,
            DurationDays = Math.Max(1, rule.DurationDays),
            IsNonWorkingDay = rule.IsNonWorkingDay ?? false,
            CalendarType = rule.CalendarType,
            TerritoryCode = rule.TerritoryCode,
            Tags = rule.Tags,
            Comment = rule.Comment,
        };

        return new ResolvedNotableDateOccurrence(
            rule,
            date.Date,
            rule.TerritoryCode,
            notable);
    }
}
