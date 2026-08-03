// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests for the sealed notable-date binary rule-pack format: round-trip fidelity, byte stability, and the rejection
/// of malformed input.
/// </summary>
[TestClass]
public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>
    /// Writes a resource to a pack in memory.
    /// </summary>
    /// <param name="resource">The resource to encode.</param>
    /// <returns>The pack bytes.</returns>
    private static byte[] WritePack(NotableDateResource resource)
    {
        using MemoryStream stream = new();
        NotableDateBinaryResource.Write(resource, stream);

        return stream.ToArray();
    }

    /// <summary>
    /// Reads a resource back from pack bytes.
    /// </summary>
    /// <param name="pack">The pack bytes.</param>
    /// <returns>The decoded resource.</returns>
    private static NotableDateResource ReadPack(byte[] pack)
    {
        using MemoryStream stream = new(pack);

        return NotableDateBinaryResource.Read(stream);
    }

    /// <summary>
    /// Computes a resolved-occurrence fingerprint over every supported territory for 2024-2026, so two resources can
    /// be compared by behaviour rather than by reference.
    /// </summary>
    /// <param name="resource">The resource to fingerprint.</param>
    /// <returns>The ordered fingerprint tuples.</returns>
    private static List<(string Territory, string Id, DateOnly Date, bool Observed, int Duration)> Fingerprint(NotableDateResource resource)
    {
        NotableDateService service = new(resource);
        List<(string, string, DateOnly, bool, int)> rows = new();

        IReadOnlyList<string> territories = service.GetSupportedTerritories();
        foreach (string territory in territories.Count == 0 ? ["XX"] : territories)
        {
            foreach (NotableDate occurrence in service.Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2026, 12, 31)), territory))
                rows.Add((territory, occurrence.NotableDateId, occurrence.Date, occurrence.IsObserved, occurrence.DurationDays));
        }

        return rows;
    }

    /// <summary>
    /// Builds a synthetic resource exercising every strategy, recurrence, and duration discriminator plus the full
    /// adjustment-policy, scope, applicability, and resolution-policy field surface.
    /// </summary>
    /// <returns>The comprehensive resource.</returns>
    private static NotableDateResource ComprehensiveResource()
    {
        ResolutionPolicy resolutionPolicy = new(
            DuplicatePolicy.KeepFirst,
            CollisionPolicy.HighestPriorityOnly,
            CollisionPolicy.CategoryPriority,
            PriorityDirection.LowerWins,
            ObservedDateRangePolicy.ActualOccurrenceControlsInclusion,
            new WeekPattern(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday),
            [NotableDateCategory.Observance, NotableDateCategory.PublicHoliday]);

        AdjustmentPolicy fullPolicy = new(
            "full-policy",
            priority: -5,
            new AdjustmentScope(
                ["AU", "NZ"],
                [CalendarSystem.Gregorian, CalendarSystem.Hebrew],
                [NotableDateCategory.PublicHoliday],
                ["nd-fixed"],
                ["nd-fixed/rule-fixed"],
                fromYear: 2000,
                toYear: 2100,
                onlyYears: [2024, 2026],
                exceptYears: [2025]),
            AdjustmentTrigger.IfWeekend,
            [DayOfWeek.Saturday, DayOfWeek.Sunday],
            AdjustmentAction.MoveToNextWorkingDay,
            actionWeekday: DayOfWeek.Monday,
            actionDays: -2,
            maxSearchDays: 14,
            skipWeekends: true,
            skipNonWorkingDates: true,
            EmissionMode.ActualAndObserved,
            reason: "Substitute day",
            nonWorking: true,
            triggerMonth: 12,
            triggerDay: 26,
            triggerWeekOrdinal: WeekOrdinal.Second,
            actionNotableDateRef: "nd-fixed",
            actionRuleRef: "rule-fixed",
            actionHandlerKey: "action-handler",
            triggerHandlerKey: "trigger-handler",
            handlerParameters: new Dictionary<string, string> { ["b-key"] = "2", ["a-key"] = "1" });

        AdjustmentPolicy minimalPolicy = new(
            "minimal-policy",
            priority: 0,
            AdjustmentScope.Global,
            AdjustmentTrigger.Always,
            [],
            AdjustmentAction.None,
            actionWeekday: null,
            actionDays: 0,
            maxSearchDays: null,
            skipWeekends: false,
            skipNonWorkingDates: false,
            EmissionMode.ActualOnly,
            reason: null,
            nonWorking: null);

        RuleApplicability fullApplicability = new(
            CalendarSystem.Hebrew,
            fromYear: 1990,
            toYear: 2100,
            territories: ["AU", "AU-VIC"],
            onlyYears: [2024],
            exceptYears: [2025, 2027],
            everyYears: 2,
            anchorYear: 2024);

        RuleApplicability defaultApplicability = new(CalendarSystem.Gregorian, null, null, [], [], []);

        NotableDateRule Rule(string id, IDateCalculationStrategy strategy, RuleApplicability? applicability = null, NotableDateDurationDefinition? duration = null, int priority = 100) =>
            new(id, priority, NotableDateCategory.Observance, true, duration, applicability ?? defaultApplicability, strategy, ["full-policy"], ["tag-a", "tag-b"]);

        List<NotableDateRule> strategyRules =
        [
            Rule("rule-fixed", new FixedDateStrategy(1, 15, CalendarSystem.Hebrew, sweepCalendarYears: true, skipLeapMonth: false, monthAlias: "Nisan"), fullApplicability, new FixedDurationDefinition(8), priority: -3),
            Rule("rule-dowim", new DayOfWeekInMonthStrategy(9, DayOfWeek.Monday, WeekOrdinal.First)),
            Rule("rule-relative", new RelativeWeekdayInMonthStrategy(5, DayOfWeek.Friday, WeekOrdinal.Last, DayOfWeek.Tuesday, WeekdayProximity.After)),
            Rule("rule-near-date", new WeekdayNearDateStrategy(6, 15, DayOfWeek.Wednesday, WeekdayProximity.Nearest)),
            Rule("rule-near-rule", new WeekdayNearRuleStrategy("nd-fixed", "rule-fixed", DayOfWeek.Thursday, WeekdayProximity.OnOrAfter, referenceYearOffset: -1)),
            Rule("rule-offset", new OffsetFromRuleStrategy("nd-fixed", null, offsetDays: -49)),
            Rule("rule-nth", new NthWeekdayFromRuleStrategy("nd-fixed", "rule-fixed", DayOfWeek.Sunday, ordinal: 3, referenceYearOffset: 1)),
            Rule("rule-working-offset", new WorkingDayOffsetFromRuleStrategy("nd-fixed", null, offsetWorkingDays: 5, referenceYearOffset: 0)),
            Rule("rule-working-month", new WorkingDayInMonthStrategy(3, 1)),
            Rule("rule-ordinal-day", new OrdinalDayOfMonthStrategy(11, 30)),
            Rule("rule-iso-week", new IsoWeekDateStrategy(52, DayOfWeek.Friday)),
            Rule("rule-day-of-year", new DayOfYearStrategy(256)),
            Rule(
                "rule-algorithm",
                new AlgorithmDateStrategy("western-easter"),
                duration: new CalculatedEndDateDurationDefinition(
                    new OffsetFromRuleStrategy("nd-fixed", "rule-fixed", 2),
                    DateBoundary.Exclusive,
                    DateBoundary.Inclusive,
                    EndDateSelection.FirstAfterStart)),
        ];

        List<NotableDateRule> recurrenceRules =
        [
            new("rec-daily", 1, null, null, null, defaultApplicability, new DailyIntervalRecurrenceStrategy(new DateOnly(2024, 1, 1), intervalDays: 14), [], []),
            new("rec-weekly", 2, null, false, null, defaultApplicability, new WeeklyRecurrenceStrategy([DayOfWeek.Monday, DayOfWeek.Thursday], intervalWeeks: 2, anchorDate: new DateOnly(2024, 1, 4)), [], ["weekly"]),
            new("rec-monthly-day", 3, NotableDateCategory.Observance, null, null, defaultApplicability, new MonthlyDayRecurrenceStrategy(31, intervalMonths: 1, anchorDate: null, invalidDayBehavior: InvalidDayOfMonthBehavior.UseLastDayOfMonth), [], []),
            new("rec-monthly-weekday", 4, null, null, null, defaultApplicability, new MonthlyWeekdayRecurrenceStrategy(DayOfWeek.Friday, WeekOrdinal.Third, intervalMonths: 3, anchorDate: new DateOnly(2024, 2, 16)), [], []),
        ];

        List<NotableDateDefinition> definitions =
        [
            new("nd-fixed", "Every Strategy", NotableDateCategory.PublicHoliday, defaultNonWorkingDay: true, defaultDurationDays: 2, ["concept-tag"], strategyRules),
            new("nd-recurring", "Every Recurrence", NotableDateCategory.Observance, defaultNonWorkingDay: false, defaultDurationDays: 1, [], recurrenceRules),
        ];

        return new NotableDateResource("test.binary.comprehensive", "1.0", resolutionPolicy, [fullPolicy, minimalPolicy], definitions);
    }
}
