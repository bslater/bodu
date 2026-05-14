// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceDynamicExpansionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests dynamic blocker expansion in the <see cref="NotableDateResolutionService" />.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceDynamicExpansionTests
{
    /// <summary>
    /// Verifies that a cross-year substitute skips a neighbouring-year fixed non-working day.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCrossYearSubstituteConflictsWithNextYearHoliday_ShouldShiftToNextAvailableDate()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        NotableDateResolutionRequest request = new(
            new DateTime(2022, 12, 1),
            new DateTime(2023, 3, 31),
            NotableDateResolutionProjection.AnchorDate);

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        NotableDate observed = actual.Single(date =>
            date.Name == "Year-End Holiday" &&
            date.WasAdjusted);

        Assert.AreEqual(new DateTime(2023, 1, 3), observed.Date);

        Assert.IsFalse(actual.Any(date =>
            date.Name == "Year-End Holiday" &&
            date.WasAdjusted &&
            date.Date == new DateTime(2023, 1, 2)));
    }

    /// <summary>
    /// Verifies that blocker dates materialised by expansion are not emitted to the original request output.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenExpansionLoadsNextYearBlocker_ShouldNotEmitBlockerToOriginalYearOutput()
    {
        NotableDateResolutionService service = CreateService(
            FixedPublicHolidayRule("Year-End Holiday", month: 12, day: 31),
            FixedNonWorkingRule("New Year Second Day", month: 1, day: 2));

        NotableDateResolutionRequest request = new(
            new DateTime(2022, 1, 1),
            new DateTime(2022, 12, 31),
            NotableDateResolutionProjection.AnchorDate);

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        Assert.IsFalse(actual.Any(date =>
            date.Name == "New Year Second Day" &&
            date.Date == new DateTime(2023, 1, 2)));
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

    private static NotableDateRule FixedNonWorkingRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray<ObservanceAdjustment>.Empty,
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
