// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceAdjustmentTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests adjustment behaviour through <see cref="NotableDateResolutionService" />.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceAdjustmentTests
{
    /// <summary>
    /// Verifies that the new service emits adjacent substitute days for adjacent weekend holidays.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAdjacentWeekendHolidaysShiftForward_ShouldAllocateDistinctSubstituteDays()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Christmas Day", month: 12, day: 25),
            FixedPublicHolidayRule("Boxing Day", month: 12, day: 26));

        NotableDateResolutionRequest request = new(
            new DateTime(2021, 12, 24),
            new DateTime(2021, 12, 31),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

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
    /// Verifies that both actual and observed occurrences are emitted for an adjusted holiday.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenHolidayIsAdjusted_ShouldReturnActualAndObservedOccurrences()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Christmas Day", month: 12, day: 25));

        NotableDateResolutionRequest request = new(
            new DateTime(2021, 12, 24),
            new DateTime(2021, 12, 31),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Christmas Day" &&
            date.Date == new DateTime(2021, 12, 25) &&
            !date.WasAdjusted));

        Assert.IsTrue(actual.Any(date =>
            date.Name == "Christmas Day" &&
            date.Date == new DateTime(2021, 12, 27) &&
            date.WasAdjusted));
    }

    private static NotableDateResolutionService CreateService(params NotableDateRule[] rules) =>
        new(ruleProviders: new[] { new InMemoryRuleProvider(rules) });

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
