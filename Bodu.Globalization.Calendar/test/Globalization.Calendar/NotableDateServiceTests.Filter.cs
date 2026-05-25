// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Filter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Houses the bespoke filter-integration tests for <see cref="NotableDateService" /> that do not fit the
/// "year + filter → expected name set" KAT consolidation in
/// <see cref="NotableDateServiceTests.FilterScenarios" />. The kept tests assert ordering, cache invariants,
/// exception contracts, adjustment-flag behaviour, count-only data rows, range-/single-date API surface, and
/// the XML-driven fixtures that share rule definitions with <see cref="NotableDateRuleParserTests" />.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    private static NotableDateRule FixedWithTags(string name, int month, int day, NotableDateCategory category, bool nonWorking, ImmutableHashSet<string> tags) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = category,
            Month = month,
            Day = day,
            IsNonWorkingDay = nonWorking,
            Tags = tags,
        };

    private static NotableDateService BuildServiceFromXml(string xml)
    {
        NotableDateRule[] rules = [.. NotableDateRuleParser.ParseXml(xml)];
        return BuildService(rules);
    }

    // --------------------------------------------------------------------------------------
    // GetNotableDates(year, filter) — kept bespoke
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" />
    /// returns results ordered by anchor date. The ordering contract cannot be encoded as a set equivalence,
    /// so the test stays bespoke.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndFilter_ShouldReturnResultsOrderedByDate()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday C", 12, 25, NotableDateCategory.Holiday),
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Holiday B", 7, 4, NotableDateCategory.Holiday));

        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results[0].Date <= results[1].Date && results[1].Date <= results[2].Date);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" />
    /// throws <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndNullFilter_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(2024, (NotableDateFilter)null!);
        });
    }

    /// <summary>
    /// Verifies that a filtered query does not affect subsequent unfiltered queries, confirming that the
    /// per-year cache remains intact and returns complete results after a filtered call. The two-call cache
    /// invariant is a stateful contract that cannot be expressed as a single per-row KAT.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndFilter_ShouldNotPolluteCacheForUnfilteredQuery()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance B", 6, 1, NotableDateCategory.Observance));

        IReadOnlyList<NotableDate> filtered = service.GetNotableDates(2024, NotableDateFilter.ForCategory(NotableDateCategory.Holiday));
        IReadOnlyList<NotableDate> unfiltered = service.GetNotableDates(2024);

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual(2, unfiltered.Count);
    }

    // --------------------------------------------------------------------------------------
    // GetNotableDates(startDate, endDate, filter)
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
    /// returns only dates within the range that also match the filter. Different API surface from the
    /// year-based query consolidated by <see cref="FilterScenarios" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithDateRangeAndCategoryFilter_ShouldReturnMatchingDatesInRange()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday Jan", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance Mar", 3, 15, NotableDateCategory.Observance),
            Fixed("Holiday Dec", 12, 25, NotableDateCategory.Holiday));

        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(
            new DateTime(2024, 1, 1), new DateTime(2024, 6, 30), filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Holiday Jan", results[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
    /// throws <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithDateRangeAndNullFilter_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), (NotableDateFilter)null!);
        });
    }

    // --------------------------------------------------------------------------------------
    // GetNotableDates(date, filter)
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" />
    /// returns the notable date on that day when the filter matches.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithSingleDateAndMatchingFilter_ShouldReturnDate()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance B", 1, 1, NotableDateCategory.Observance));

        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(new DateTime(2024, 1, 1), filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Holiday A", results[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" />
    /// returns an empty list when the filter excludes all dates on that day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithSingleDateAndNonMatchingFilter_ShouldReturnEmptyList()
    {
        NotableDateService service = BuildService(
            Fixed("Observance B", 1, 1, NotableDateCategory.Observance));

        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(new DateTime(2024, 1, 1), filter);

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" />
    /// throws <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithSingleDateAndNullFilter_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(new DateTime(2024, 1, 1), (NotableDateFilter)null!);
        });
    }

    // --------------------------------------------------------------------------------------
    // WasAdjusted filter — asserts the WasAdjusted flag on the returned occurrence, not just names.
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> returns only the adjusted occurrence when
    /// an observance adjustment fires. The assertion checks <c>WasAdjusted</c> and the resolved date on the
    /// returned <see cref="NotableDate" />, not its name, so the test stays bespoke.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndWasAdjustedFilter_WhenAdjustmentFires_ShouldReturnOnlyAdjustedOccurrence()
    {
        // 1 January 2022 is a Saturday; the IfWeekend trigger fires and moves it to Monday 3 January.
        NotableDateRule rule = Fixed("New Year's Day", 1, 1, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "weekend-roll",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }),
        };

        NotableDateService service = BuildService(rule);
        var filter = NotableDateFilter.WasAdjusted();
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2022, filter);

        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(results[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2022, 1, 3), results[0].Date);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> returns an empty list when no adjustment
    /// fires for the queried year. Companion to the firing-case test above; kept alongside for proximity.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndWasAdjustedFilter_WhenNoAdjustmentFires_ShouldReturnEmptyList()
    {
        // 1 January 2024 is a Monday; the IfWeekend trigger does not fire.
        NotableDateRule rule = Fixed("New Year's Day", 1, 1, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "weekend-roll",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextWeekday,
            }),
        };

        NotableDateService service = BuildService(rule);
        var filter = NotableDateFilter.WasAdjusted();
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(0, results.Count);
    }

    // --------------------------------------------------------------------------------------
    // ForAnyCategory — already [DataRow]-driven, asserts count rather than names.
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> returns dates belonging to any of the
    /// supplied categories and excludes all others. The assertion checks the result count parameterised
    /// over <see cref="NotableDateCategory" /> pairs and stays as a focused per-row data table here.
    /// </summary>
    /// <param name="categoryA">The first category passed to <see cref="NotableDateFilter.ForAnyCategory" />.</param>
    /// <param name="categoryB">The second category passed to the filter.</param>
    /// <param name="expectedCount">The expected number of occurrences after filtering.</param>
    [DataRow(NotableDateCategory.Holiday, NotableDateCategory.Observance, 2)]
    [DataRow(NotableDateCategory.Holiday, NotableDateCategory.Cultural, 2)]
    [DataRow(NotableDateCategory.Observance, NotableDateCategory.Seasonal, 2)]
    [DataRow(NotableDateCategory.Remembrance, NotableDateCategory.Other, 0)]
    [TestMethod]
    public void GetNotableDates_WithYearAndForAnyCategoryFilter_ShouldReturnExpectedCount(
        NotableDateCategory categoryA,
        NotableDateCategory categoryB,
        int expectedCount)
    {
        // Service has exactly one Holiday, one Observance, one Cultural, and one Seasonal rule.
        NotableDateService service = BuildService(
            Fixed("Holiday", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance", 3, 15, NotableDateCategory.Observance),
            Fixed("Cultural", 6, 1, NotableDateCategory.Cultural),
            Fixed("Seasonal", 9, 22, NotableDateCategory.Seasonal));

        var filter = NotableDateFilter.ForAnyCategory(categoryA, categoryB);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(expectedCount, results.Count);
    }

    // --------------------------------------------------------------------------------------
    // XML-driven integration — known static XML fragments from NotableDateRuleParserTests
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> correctly filters dates produced from rules
    /// parsed from a known static XML fragment, confirming that tags authored in XML are honoured by the
    /// filter pipeline.
    /// </summary>
    /// <param name="tag">The tag passed to <see cref="NotableDateFilter.WithTag" />.</param>
    /// <param name="expectsDate"><see langword="true" /> when the tag should match the parsed rule;
    /// <see langword="false" /> when it should not.</param>
    [DataRow("Public", true)]
    [DataRow("Civic", true)]
    [DataRow("PUBLIC", true)]
    [DataRow("civic", true)]
    [DataRow("Regional", false)]
    [DataRow("Religious", false)]
    [TestMethod]
    public void GetNotableDates_UsingParsedFixedRuleXml_WithTagFilter_ShouldMatchExpected(string tag, bool expectsDate)
    {
        NotableDateService service = BuildServiceFromXml(NotableDateRuleParserTests.FixedRuleXml);
        var filter = NotableDateFilter.WithTag(tag);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter, territoryCode: "AU-NSW");

        Assert.AreEqual(expectsDate ? 1 : 0, results.Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> correctly identifies the named date produced
    /// from a known static XML fragment.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_UsingParsedFixedRuleXml_WithNameFilter_ShouldReturnMatchingDate()
    {
        NotableDateService service = BuildServiceFromXml(NotableDateRuleParserTests.FixedRuleXml);
        var filter = NotableDateFilter.WithName("Fixed Rule Test");
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter, territoryCode: "AU-NSW");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Fixed Rule Test", results[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> applied to a service built from
    /// <see cref="NotableDateRuleParserTests.MultiRuleXml" /> returns the fixed New Year's Day rule for the
    /// Holiday category while the Algorithm-based Easter Sunday rule — which cannot resolve without a
    /// registered algorithm — is silently omitted.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_UsingParsedMultiRuleXml_WithHolidayCategoryFilter_ShouldContainNewYearsDay()
    {
        NotableDateService service = BuildServiceFromXml(NotableDateRuleParserTests.MultiRuleXml);
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.IsTrue(results.Any(d => d.Name == "New Year's Day"), "New Year's Day must appear as the only resolvable Holiday in the multi-rule XML set.");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> applied to a service built from
    /// <see cref="NotableDateRuleParserTests.MultiRuleXml" /> returns only dates flagged non-working,
    /// confirming that the XML <c>nonWorking</c> attribute flows through to the filter pipeline.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_UsingParsedMultiRuleXml_WithNonWorkingFilter_ShouldReturnOnlyNonWorkingDates()
    {
        NotableDateService service = BuildServiceFromXml(NotableDateRuleParserTests.MultiRuleXml);
        var filter = NotableDateFilter.IsNonWorkingDay();
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.IsTrue(results.Count > 0, "Expected at least one non-working date from MultiRuleXml.");
        Assert.IsTrue(results.All(d => d.IsNonWorkingDay), "Every returned date must be flagged as a non-working day.");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> applied to a service built from a known
    /// XML fragment correctly returns the date when every required tag is present.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_UsingParsedFixedRuleXml_WithAllTagsFilter_WhenBothTagsPresent_ShouldReturnDate()
    {
        NotableDateService service = BuildServiceFromXml(NotableDateRuleParserTests.FixedRuleXml);
        var filter = NotableDateFilter.WithAllTags("Public", "Civic");
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter, territoryCode: "AU-NSW");

        Assert.AreEqual(1, results.Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> returns no dates when one of the required
    /// tags is absent from the rule parsed from a known XML fragment.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_UsingParsedFixedRuleXml_WithAllTagsFilter_WhenOneTagAbsent_ShouldReturnEmpty()
    {
        NotableDateService service = BuildServiceFromXml(NotableDateRuleParserTests.FixedRuleXml);
        var filter = NotableDateFilter.WithAllTags("Public", "Religious");
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter, territoryCode: "AU-NSW");

        Assert.AreEqual(0, results.Count);
    }
}
