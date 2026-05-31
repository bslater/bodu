// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Globalization;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the end-to-end behaviour of <see cref="NotableDateService" /> across rule resolution, adjustments, overrides, multi-day
/// spans, and cache invalidation.
/// </summary>
[TestClass]
public sealed partial class NotableDateServiceTests
{
    private static NotableDateRule Fixed(string name, int month, int day, NotableDateCategory category = NotableDateCategory.Holiday, bool nonWorking = false, string? territory = null) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = category,
            Month = month,
            Day = day,
            IsNonWorkingDay = nonWorking,
            TerritoryCode = territory,
        };

    private sealed class InMemoryRuleProvider
        : INotableDateRuleProvider
    {
        private readonly IEnumerable<NotableDateRule> _rules;

        public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }

    private static NotableDateService BuildService(params NotableDateRule[] rules) =>
        new([(INotableDateRuleProvider)new InMemoryRuleProvider(rules)], WorkingDaysOfWeek.MondayToFriday);

    /// <summary>
    /// Verifies that querying a year returns every notable date defined for that year in chronological order.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingYear_ShouldReturnChronologicalDates()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Christmas Day", 12, 25));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2026);

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(new DateTime(2026, 1, 1), results[0].Date);
        Assert.AreEqual(new DateTime(2026, 12, 25), results[1].Date);
    }

    /// <summary>
    /// Verifies that an adjustment that rolls a Saturday onto Monday emits a single occurrence carrying the post-adjustment
    /// observed date and an <see cref="AdjustmentReason" /> describing the original anchor. The range pipeline emits one
    /// entry per rule occurrence — the adjusted one when an adjustment fires — rather than the legacy pair of
    /// (original, adjusted).
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenWeekendRollAdjustmentFires_ShouldExposeAdjustmentReason()
    {
        NotableDateRule rule = Fixed("New Year's Day", 1, 1, nonWorking: true) with
        {
            Adjustments = [new ObservanceAdjustment
            {
                Key = "test",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }],
        };

        NotableDateService service = BuildService(rule);

        // 1 January 2022 is a Saturday.
        var results = service.GetNotableDates(2022).Where(d => d.Name == "New Year's Day").ToList();

        Assert.AreEqual(1, results.Count);
        NotableDate adjusted = results[0];
        Assert.IsTrue(adjusted.WasAdjusted);
        Assert.AreEqual(DayOfWeek.Monday, adjusted.Date.DayOfWeek);
        Assert.AreEqual(new DateTime(2022, 1, 1), adjusted.AdjustmentReason!.OriginalDate);
        Assert.AreEqual(AdjustmentTrigger.IfWeekend, adjusted.AdjustmentReason.Trigger);
    }

    /// <summary>
    /// Verifies that querying a single day inside a multi-day span returns the span anchored on a previous day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMultiDaySpanCoversQueryDay_ShouldReturnSpan()
    {
        NotableDateRule span = Fixed("Festival", 6, 1) with { DurationDays = 5 };
        NotableDateService service = BuildService(span);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(new DateTime(2025, 6, 3));

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Festival", results[0].Name);
        Assert.AreEqual(new DateTime(2025, 6, 1), results[0].Date);
        Assert.AreEqual(new DateTime(2025, 6, 5), results[0].EndDate);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.IsNonWorkingDay" /> returns <see langword="true" /> for a weekend.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenSaturday_ShouldReturnTrue()
    {
        NotableDateService service = BuildService();

        Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2025, 1, 4)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.IsNonWorkingDay" /> returns <see langword="true" /> when a notable date marked
    /// non-working falls on the requested day.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenNotableHolidayOnDay_ShouldReturnTrue()
    {
        NotableDateService service = BuildService(Fixed("Christmas Day", 12, 25, nonWorking: true));

        Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2025, 12, 25)));
    }

    /// <summary>
    /// Verifies that querying with a country territory matches a rule scoped to that country's subdivision.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenTerritoryMatchesSubdivision_ShouldReturnRule()
    {
        NotableDateRule rule = Fixed("Picnic Day", 8, 4, territory: "AU-NT");
        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> auNt = service.GetNotableDates(2025, territoryCode: "AU-NT");
        IReadOnlyList<NotableDate> au = service.GetNotableDates(2025, territoryCode: "AU");

        Assert.AreEqual(1, auNt.Count);
        Assert.AreEqual(1, au.Count);
    }

    /// <summary>
    /// Verifies that querying with an unrelated territory excludes the rule.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenTerritoryUnrelated_ShouldExcludeRule()
    {
        NotableDateRule rule = Fixed("Bastille Day", 7, 14, territory: "FR");
        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> us = service.GetNotableDates(2025, territoryCode: "US");

        Assert.AreEqual(0, us.Count);
    }

    /// <summary>
    /// Verifies that an override provider can suppress a base rule for a specific year window.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenOverrideRemovesRuleForYear_ShouldExcludeRule()
    {
        NotableDateRule baseRule = Fixed("Holiday", 1, 1);
        var override_ = new TestOverrideProvider(
            removals: [new RuleRemoval("Holiday", FromYear: 2025, ToYear: 2025)],
            additions: []);

        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(baseRule)],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)override_],
            });

        Assert.AreEqual(0, service.GetNotableDates(2025).Count);
        Assert.AreEqual(1, service.GetNotableDates(2026).Count);
    }

    /// <summary>
    /// Verifies that an override provider can add a one-off notable date.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenOverrideAddsRule_ShouldExposeRule()
    {
        NotableDateRule addition = Fixed("Royal Wedding", 5, 19, NotableDateCategory.Cultural);
        var override_ = new TestOverrideProvider(
            removals: [],
            additions: [addition]);

        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Holiday", 1, 1))],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)override_],
            });

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);
        Assert.IsTrue(results.Any(r => r.Name == "Royal Wedding"));
    }

    // -----------------------------------------------------------------------------------------
    // Constructor argument validation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="NotableDateService" /> constructor throws
    /// <see cref="ArgumentNullException" /> when <paramref name="ruleProviders" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRuleProvidersIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDateService(
                ruleProviders: null!,
                WorkingDaysOfWeek.MondayToFriday);
        });

        Assert.AreEqual("ruleProviders", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the <see cref="NotableDateService" /> constructor rejects undefined
    /// <see cref="WorkingDaysOfWeek" /> values via
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [DataRow(-1)]
    [DataRow(100)]
    [TestMethod]
    public void Ctor_WhenWeekendDefinitionIsUndefined_ShouldThrowExactly(int undefined)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new NotableDateService(
                [],
                (WorkingDaysOfWeek)undefined);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="NotableDateService" /> constructor rejects
    /// <see cref="WorkingDaysOfWeek.Custom" /> because it has no canonical <see cref="WeekPattern" />;
    /// callers must convert their <see cref="IWeekendDefinitionProvider" /> to a <see cref="WeekPattern" /> first.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWeekendDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NotableDateService(
                [],
                WorkingDaysOfWeek.Custom);
        });
    }

    /// <summary>
    /// Verifies that the parameterless constructor loads the embedded minimal default XML resource successfully and yields the
    /// single bundled rule (New Year's Day) for a common year.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldLoadEmbeddedDefaultMinimalResource()
    {
        var service = new NotableDateService();

        IReadOnlyList<NotableDate> result = service.GetNotableDates(2026);

        Assert.IsTrue(result.Count > 0, "expected the embedded default-minimal.xml to produce at least one rule for 2026");
        Assert.IsTrue(result.Any(d => d.Name == "New Year's Day"));
    }

    /// <summary>
    /// Verifies that the parameterless constructor surfaces New Year's Day on 1 January and that no region-specific holiday
    /// (for example Independence Day on 4 July) leaks into the minimal default — those rules must come from a companion data
    /// pack such as <c>Bodu.Globalization.Calendar.Data.Americas</c>.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenParameterless_ShouldReturnNewYearsDayAndExcludeRegionalHolidays_ForYear2026()
    {
        var service = new NotableDateService();

        IReadOnlyList<NotableDate> newYears = service.GetNotableDates(new DateTime(2026, 1, 1));
        IReadOnlyList<NotableDate> july4 = service.GetNotableDates(new DateTime(2026, 7, 4));

        Assert.IsTrue(newYears.Any(d => d.Name == "New Year's Day"));
        Assert.IsFalse(july4.Any(d => d.Name == "Independence Day"),
            "Region-specific rules must not appear in the minimal default; they ship in companion data packs.");
    }

    // -----------------------------------------------------------------------------------------
    // Query / filter edge cases
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(DateTime, DateTime, string?, Type?)" /> accepts a
    /// well-ordered range without throwing — complementing the strict reversed-range contract pinned in the
    /// <c>ReversedRange</c> partial.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRangeIsWellOrdered_ShouldReturnExpectedRange()
    {
        NotableDateService service = BuildService(Fixed("Midsummer", 6, 21));

        IReadOnlyList<NotableDate> forward = service.GetNotableDates(new DateTime(2025, 6, 1), new DateTime(2025, 6, 30));

        Assert.AreEqual(1, forward.Count);
        Assert.AreEqual(new DateTime(2025, 6, 21), forward[0].Date);
    }

    /// <summary>
    /// Verifies that a multi-day span starting in the previous year is returned when querying
    /// a single day inside the wrap-around portion in the subsequent year.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenSpanWrapsOverYearBoundary_ShouldReturnSpanFromPreviousYear()
    {
        // A 10-day festival starting 28 December 2024 extends into 6 January 2025.
        NotableDateRule rule = Fixed("New Year Festival", 12, 28) with { DurationDays = 10 };
        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> januaryQuery = service.GetNotableDates(new DateTime(2025, 1, 2));

        Assert.AreEqual(1, januaryQuery.Count);
        Assert.AreEqual(new DateTime(2024, 12, 28), januaryQuery[0].Date);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(DateTime, string?, Type?)" />
    /// for year 1 skips the (invalid) year-0 lookup rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingSingleDayOfYear1_ShouldNotThrow()
    {
        NotableDateService service = BuildService(Fixed("Anchor", 1, 2));

        IReadOnlyList<NotableDate> result = service.GetNotableDates(new DateTime(1, 1, 2));

        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Verifies that the calendar-type filter excludes rules whose calendar type does not match
    /// the requested type.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenCalendarTypeFilterDoesNotMatchRule_ShouldExcludeRule()
    {
        NotableDateRule gregorian = Fixed("Christmas", 12, 25) with { CalendarType = typeof(GregorianCalendar) };
        NotableDateService service = BuildService(gregorian);

        IReadOnlyList<NotableDate> julianResult = service.GetNotableDates(2025, calendarType: typeof(JulianCalendar));

        Assert.AreEqual(0, julianResult.Count);
    }

    /// <summary>
    /// Verifies that a malformed territory query returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRequestedTerritoryIsMalformed_ShouldReturnEmpty()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 1, territory: "AU-NSW"));

        IReadOnlyList<NotableDate> result = service.GetNotableDates(2025, territoryCode: "BAD!");

        Assert.AreEqual(0, result.Count);
    }

    // -----------------------------------------------------------------------------------------
    // Override truth table
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the year bounds on a <see cref="RuleRemoval" /> gate the suppression: only
    /// years within <c>FromYear..ToYear</c> are suppressed, years outside the window are not.
    /// </summary>
    [DataRow(2023, false)]
    [DataRow(2024, true)]
    [DataRow(2025, true)]
    [DataRow(2026, false)]
    [TestMethod]
    public void Overrides_RuleRemoval_YearBounds(int year, bool expectedSuppressed)
    {
        NotableDateRule baseRule = Fixed("Holiday", 1, 1);
        var removal = new RuleRemoval("Holiday", FromYear: 2024, ToYear: 2025);
        var provider = new TestOverrideProvider([removal], []);

        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(baseRule)],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)provider],
            });

        var count = service.GetNotableDates(year).Count(r => r.Name == "Holiday");

        Assert.AreEqual(expectedSuppressed ? 0 : 1, count);
    }

    /// <summary>
    /// Verifies that the territory scope on a <see cref="RuleRemoval" /> honours
    /// parent-contains-subdivision semantics: a removal scoped to <c>AU</c> suppresses the rule
    /// for every <c>AU-*</c> subdivision; a removal scoped to <c>AU-NSW</c> suppresses only
    /// that exact subdivision.
    /// </summary>
    [DataRow("AU", "AU-NSW", true)]
    [DataRow("AU", "AU-QLD", true)]
    [DataRow("AU-NSW", "AU-NSW", true)]
    [DataRow("AU-NSW", "AU-QLD", false)]
    [TestMethod]
    public void Overrides_RuleRemoval_TerritoryScope(string removalTerritory, string ruleTerritory, bool expectedSuppressed)
    {
        NotableDateRule baseRule = Fixed("Picnic", 8, 4, territory: ruleTerritory);
        var removal = new RuleRemoval("Picnic", TerritoryCode: removalTerritory);
        var provider = new TestOverrideProvider([removal], []);

        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(baseRule)],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)provider],
            });

        var count = service.GetNotableDates(2025, territoryCode: ruleTerritory).Count(r => r.Name == "Picnic");

        Assert.AreEqual(expectedSuppressed ? 0 : 1, count);
    }

    /// <summary>
    /// Verifies that a territory-scoped removal does not fire when the rule itself has no
    /// territory (i.e. is global). This matches the production guard that treats an empty
    /// generation-context territory as "no match for any territory-scoped removal".
    /// </summary>
    [TestMethod]
    public void Overrides_RuleRemoval_WhenRemovalTerritoryButRuleHasNone_ShouldNotSuppress()
    {
        NotableDateRule baseRule = Fixed("Holiday", 1, 1);
        var removal = new RuleRemoval("Holiday", TerritoryCode: "AU-NSW");
        var provider = new TestOverrideProvider([removal], []);

        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(baseRule)],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)provider],
            });

        var count = service.GetNotableDates(2025).Count(r => r.Name == "Holiday");

        Assert.AreEqual(1, count);
    }

    /// <summary>
    /// Verifies that a malformed removal territory is ignored (never suppresses) rather than
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Overrides_WhenRemovalTerritoryIsMalformed_ShouldNotSuppress()
    {
        NotableDateRule baseRule = Fixed("Picnic", 8, 4, territory: "AU-NSW");
        var removal = new RuleRemoval("Picnic", TerritoryCode: "NOT-A-REAL-CODE-!");
        var provider = new TestOverrideProvider([removal], []);

        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(baseRule)],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)provider],
            });

        IReadOnlyList<NotableDate> result = service.GetNotableDates(2025, territoryCode: "AU-NSW");

        Assert.AreEqual(1, result.Count);
    }

    // -----------------------------------------------------------------------------------------
    // Localiser integration
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the localiser is invoked and its returned name replaces the rule's name on
    /// the emitted <see cref="NotableDate" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenLocaliserReturnsDifferentName_ShouldReplaceName()
    {
        var localiser = new DelegateLocaliser((notable, culture) => "Jour de l'An");
        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("New Year's Day", 1, 1))],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions { NameLocalizer = localiser });

        NotableDate result = service.GetNotableDates(2025)[0];

        Assert.AreEqual("Jour de l'An", result.Name);
    }

    /// <summary>
    /// Verifies that a localiser returning the same canonical name is value-equivalent across repeated queries.
    /// Reference equality across calls is no longer guaranteed because the range pipeline materialises a fresh result
    /// per request.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenLocaliserReturnsSameName_ShouldNoOpTheRecord()
    {
        var localiser = new DelegateLocaliser((notable, culture) => notable.Name);
        var service = new NotableDateService(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("New Year's Day", 1, 1))],
            WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions { NameLocalizer = localiser });

        NotableDate first = service.GetNotableDates(2025)[0];
        NotableDate second = service.GetNotableDates(2025)[0];

        Assert.AreEqual("New Year's Day", first.Name);
        Assert.AreEqual(first, second);
    }

    // -----------------------------------------------------------------------------------------
    // Cache invalidation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Invalidate()" /> clears the full cache such
    /// that subsequent queries still return correct results.
    /// </summary>
    [TestMethod]
    public void Invalidate_WhenNoYearSpecified_ShouldClearAllYears()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 1));
        _ = service.GetNotableDates(2025);
        _ = service.GetNotableDates(2026);

        service.Invalidate();

        Assert.AreEqual(1, service.GetNotableDates(2025).Count);
        Assert.AreEqual(1, service.GetNotableDates(2026).Count);
    }

    private sealed class TestOverrideProvider
        : INotableDateRuleOverrideProvider
    {
        private readonly IEnumerable<RuleRemoval> _removals;
        private readonly IEnumerable<NotableDateRule> _additions;

        public TestOverrideProvider(IEnumerable<RuleRemoval> removals, IEnumerable<NotableDateRule> additions)
        {
            _removals = removals;
            _additions = additions;
        }

        public IEnumerable<RuleRemoval> GetRemovals() => _removals;

        public IEnumerable<NotableDateRule> GetAdditions() => _additions;
    }

    /// <summary>
    /// Minimal <see cref="INotableDateNameLocalizer" /> that delegates to a callback so tests
    /// can control the localisation path.
    /// </summary>
    private sealed class DelegateLocaliser
        : INotableDateNameLocalizer
    {
        private readonly Func<NotableDate, CultureInfo?, string> _callback;

        public DelegateLocaliser(Func<NotableDate, CultureInfo?, string> callback) => _callback = callback;

        public string GetDisplayName(NotableDate notableDate, CultureInfo? culture = null) => _callback(notableDate, culture);
    }
}
