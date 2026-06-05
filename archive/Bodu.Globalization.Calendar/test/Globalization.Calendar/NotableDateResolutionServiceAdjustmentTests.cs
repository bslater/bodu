// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceAdjustmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests adjustment behaviour through the public <see cref="NotableDateService" /> range pipeline.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceAdjustmentTests
{
    /// <summary>
    /// Verifies that the service emits adjacent substitute days for adjacent weekend holidays.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAdjacentWeekendHolidaysShiftForward_ShouldAllocateDistinctSubstituteDays()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Christmas Day", month: 12, day: 25),
            FixedPublicHolidayRule("Boxing Day", month: 12, day: 26));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2021, 12, 24),
            new DateTime(2021, 12, 31));

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Christmas Day" &&
            date.Date == new DateTime(2021, 12, 27) &&
            date.WasAdjusted));

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Boxing Day" &&
            date.Date == new DateTime(2021, 12, 28) &&
            date.WasAdjusted));

        Assert.IsFalse(actual.Any(date =>
            date.Name == "Boxing Day" &&
            date.Date == new DateTime(2021, 12, 27) &&
            date.WasAdjusted));
    }

    /// <summary>
    /// Verifies that an adjusted holiday emits a single occurrence carrying the post-adjustment date and an
    /// <see cref="AdjustmentReason" /> describing the original anchor.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenHolidayIsAdjusted_ShouldEmitSingleAdjustedOccurrence()
    {
        NotableDateService service = CreateService(
            FixedPublicHolidayRule("Christmas Day", month: 12, day: 25));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2021, 12, 24),
            new DateTime(2021, 12, 31));

        NotableDate christmas = actual.Single(date => date.Name == "Christmas Day");

        Assert.AreEqual(new DateTime(2021, 12, 27), christmas.Date);
        Assert.IsTrue(christmas.WasAdjusted);
        Assert.IsNotNull(christmas.AdjustmentReason);
        Assert.AreEqual(new DateTime(2021, 12, 25), christmas.AdjustmentReason!.OriginalDate);
    }

    private static NotableDateService CreateService(params NotableDateRule[] rules) =>
        new(
            ruleProviders: [new InMemoryRuleProvider(rules)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

    private static NotableDateRule FixedPublicHolidayRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            IsNonWorkingDay = true,
            Adjustments = [new ObservanceAdjustment
                {
                    Key = "weekend-substitute",
                    Trigger = AdjustmentTrigger.IfWeekend,
                    Action = AdjustmentAction.MoveToNextWorkingDay,
                    IsNonWorkingDay = true,
                }],
        };

    private sealed class InMemoryRuleProvider
        : INotableDateRuleProvider
    {
        private readonly IReadOnlyList<NotableDateRule> _rules;

        public InMemoryRuleProvider(IReadOnlyList<NotableDateRule> rules)
        {
            this._rules = rules;
        }

        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }
}
