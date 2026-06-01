// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeResolutionKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Drives <see cref="RangeResolutionKat" /> rows against the public <see cref="NotableDateService" />
/// range-resolution surface. Each KAT row probes a (start, end, territory, calendar, rule) tuple and
/// asserts the actual-date sequence the service returns. Bespoke range-resolution coverage (observed-
/// date adjustments, multi-territory composition, calendar projection) lives in
/// <c>NotableDateRangePipelineTests.*</c>.
/// </summary>
[TestClass]
public sealed class RangeResolutionKatTests
{
    private static IReadOnlyList<RangeResolutionKat> Kats { get; } =
    [
        new(
            Name: "New Year falls within 2024 calendar year",
            Start: new DateOnly(2024, 1, 1),
            End: new DateOnly(2024, 12, 31),
            Territory: "",
            Calendar: "Gregorian",
            RuleNames: ["New Year"],
            ExpectedDates: [new DateOnly(2024, 1, 1)]),

        new(
            Name: "Christmas falls within 2024 calendar year",
            Start: new DateOnly(2024, 1, 1),
            End: new DateOnly(2024, 12, 31),
            Territory: "",
            Calendar: "Gregorian",
            RuleNames: ["Christmas"],
            ExpectedDates: [new DateOnly(2024, 12, 25)]),

        new(
            Name: "no dates fall outside the rule window",
            Start: new DateOnly(2024, 2, 1),
            End: new DateOnly(2024, 11, 30),
            Territory: "",
            Calendar: "Gregorian",
            RuleNames: ["New Year"],
            ExpectedDates: []),
    ];

    /// <summary>
    /// Verifies that each <see cref="RangeResolutionKat" /> row drives
    /// <see cref="NotableDateService.GetNotableDates(DateTime, DateTime, string?, Type?)" /> to its
    /// expected actual-date sequence. The probe builds a minimal in-memory provider carrying the
    /// Christmas and New Year rules to keep the test focused on the range-resolution invariant.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenKatRangeSpecified_ShouldYieldExpectedActualDates()
    {
        NotableDateRule[] rules =
        [
            BuildFixedRule("New Year", 1, 1),
            BuildFixedRule("Christmas", 12, 25),
        ];

        using NotableDateService service = new(
            [new InMemoryProvider(rules)],
            WorkingDaysOfWeek.MondayToFriday);

        foreach (RangeResolutionKat kat in Kats)
        {
            var start = kat.Start.ToDateTime(TimeOnly.MinValue);
            var end = kat.End.ToDateTime(TimeOnly.MaxValue);

            IReadOnlyList<NotableDate> results = service.GetNotableDates(start, end);
            IEnumerable<DateOnly> filtered = results
                .Where(r => kat.RuleNames.Contains(r.Name))
                .Select(r => DateOnly.FromDateTime(r.Date));

            CollectionAssert.AreEqual(
                kat.ExpectedDates,
                filtered.ToArray(),
                $"Range KAT '{kat.Name}': resolved dates differ from expected.");
        }
    }

    private static NotableDateRule BuildFixedRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
        };

    private sealed class InMemoryProvider : INotableDateRuleProvider
    {
        private readonly IEnumerable<NotableDateRule> _rules;

        public InMemoryProvider(IEnumerable<NotableDateRule> rules) => _rules = rules;

        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }
}
