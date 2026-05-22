// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Filter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

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
    // GetNotableDates(year, filter)
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns only dates
    /// matching the category filter and excludes all others.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndCategoryFilter_ShouldReturnOnlyMatchingCategory()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance B", 3, 15, NotableDateCategory.Observance),
            Fixed("Holiday C", 12, 25, NotableDateCategory.Holiday));

        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(d => d.Category == NotableDateCategory.Holiday));
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns no dates
    /// when the filter matches no rules.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndFilterMatchingNoRules_ShouldReturnEmptyList()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Holiday B", 12, 25, NotableDateCategory.Holiday));

        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns dates
    /// matching any of the supplied categories when an Or filter is used.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndOrCategoryFilter_ShouldReturnBothMatchingCategories()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance B", 3, 15, NotableDateCategory.Observance),
            Fixed("Cultural C", 6, 1, NotableDateCategory.Cultural));

        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .Or(NotableDateFilter.ForCategory(NotableDateCategory.Observance));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(d => d.Category == NotableDateCategory.Holiday || d.Category == NotableDateCategory.Observance));
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns only dates
    /// matching all criteria when an And filter is used.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndAndFilter_ShouldReturnIntersection()
    {
        NotableDateService service = BuildService(
            FixedWithTags("Holiday Public", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Public")),
            FixedWithTags("Holiday Private", 3, 15, NotableDateCategory.Holiday, nonWorking: false, ImmutableHashSet.Create("Regional")),
            Fixed("Observance", 6, 1, NotableDateCategory.Observance, nonWorking: true));

        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
            .And(NotableDateFilter.IsNonWorkingDay());

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Holiday Public", results[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns results
    /// ordered by anchor date.
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
    /// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> throws
    /// <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
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
    /// Verifies that a filtered query does not affect subsequent unfiltered queries, confirming that the per-year cache remains
    /// intact and returns complete results after a filtered call.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndFilter_ShouldNotPolluteCacheForUnfilteredQuery()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance B", 6, 1, NotableDateCategory.Observance));

        // Filtered call — should only return holidays.
        IReadOnlyList<NotableDate> filtered = service.GetNotableDates(2024, NotableDateFilter.ForCategory(NotableDateCategory.Holiday));

        // Unfiltered call — should return all dates including observances.
        IReadOnlyList<NotableDate> unfiltered = service.GetNotableDates(2024);

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual(2, unfiltered.Count);
    }

    // --------------------------------------------------------------------------------------
    // GetNotableDates(startDate, endDate, filter)
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
    /// returns only dates within the range that also match the filter.
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
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" /> returns the
    /// notable date on that day when the filter matches.
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
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" /> returns an
    /// empty list when the filter excludes all dates on that day.
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
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" /> throws
    /// <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
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
    // Tag filter integration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> filters service results correctly, returning only dates whose rule
    /// carries the specified tag.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithTagFilter_ShouldReturnOnlyTaggedDates()
    {
        NotableDateService service = BuildService(
            FixedWithTags("Public Holiday", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Public")),
            FixedWithTags("Regional Holiday", 6, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Regional")));

        var filter = NotableDateFilter.WithTag("Public");
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Public Holiday", results[0].Name);
    }

    // --------------------------------------------------------------------------------------
    // Name filter integration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> returns only the named dates from the service.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithAnyNameFilter_ShouldReturnOnlyNamedDates()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Easter Sunday", 4, 1),
            Fixed("Christmas Day", 12, 25));

        var filter = NotableDateFilter.WithAnyName("New Year's Day", "Christmas Day");
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(d => d.Name == "New Year's Day" || d.Name == "Christmas Day"));
    }

    // --------------------------------------------------------------------------------------
    // WithMinDuration filter integration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> returns only dates whose span equals or
    /// exceeds the minimum, excluding single-day entries.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndMinDurationFilter_ShouldReturnOnlyMultiDayDates()
    {
        NotableDateService service = BuildService(
            Fixed("Single Day", 1, 1, NotableDateCategory.Holiday),
            Fixed("Festival", 6, 1, NotableDateCategory.Cultural) with { DurationDays = 7 },
            Fixed("Long Event", 9, 1, NotableDateCategory.Observance) with { DurationDays = 14 });

        var filter = NotableDateFilter.WithMinDuration(7);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(d => d.DurationDays >= 7));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> with a minimum of one returns every
    /// date, since all dates span at least one day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndMinDurationOfOne_ShouldReturnAllDates()
    {
        NotableDateService service = BuildService(
            Fixed("Day A", 1, 1, NotableDateCategory.Holiday),
            Fixed("Day B", 6, 1, NotableDateCategory.Observance) with { DurationDays = 3 });

        var filter = NotableDateFilter.WithMinDuration(1);
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(2, results.Count);
    }

    // --------------------------------------------------------------------------------------
    // WasAdjusted filter integration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> returns only the adjusted occurrence when
    /// an observance adjustment fires, leaving the unadjusted occurrence out of the results.
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
    /// fires for the queried year.
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
    // AllOf / AnyOf compound filter integration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> returns only dates satisfying every supplied
    /// filter simultaneously.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndAllOfFilter_ShouldReturnIntersection()
    {
        NotableDateService service = BuildService(
            FixedWithTags("Public Holiday", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Public")),
            FixedWithTags("Private Holiday", 3, 15, NotableDateCategory.Holiday, nonWorking: false, ImmutableHashSet.Create("Regional")),
            FixedWithTags("Observance", 6, 1, NotableDateCategory.Observance, nonWorking: true, ImmutableHashSet.Create("Public")));

        var filter = NotableDateFilter.AllOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.IsNonWorkingDay(),
            NotableDateFilter.WithTag("Public"));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Public Holiday", results[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> returns every date matching at least one of the
    /// supplied filters.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndAnyOfFilter_ShouldReturnUnion()
    {
        NotableDateService service = BuildService(
            Fixed("Holiday", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance", 3, 15, NotableDateCategory.Observance),
            Fixed("Cultural", 6, 1, NotableDateCategory.Cultural),
            Fixed("Seasonal", 9, 22, NotableDateCategory.Seasonal));

        var filter = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            NotableDateFilter.ForCategory(NotableDateCategory.Seasonal));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(d => d.Category == NotableDateCategory.Holiday || d.Category == NotableDateCategory.Seasonal));
    }

    // --------------------------------------------------------------------------------------
    // WithAllTags / WithAnyTag integration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> returns only dates whose tag set contains
    /// every required tag, excluding dates missing any one of them.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndWithAllTagsFilter_ShouldReturnOnlyDatesWithEveryRequiredTag()
    {
        NotableDateService service = BuildService(
            FixedWithTags("Both Tags", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Public", "Federal")),
            FixedWithTags("One Tag Only", 6, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Public")),
            FixedWithTags("No Tags", 12, 25, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet<string>.Empty));

        var filter = NotableDateFilter.WithAllTags("Public", "Federal");
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Both Tags", results[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> returns every date whose tag set contains
    /// at least one of the accepted tags.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndWithAnyTagFilter_ShouldReturnDatesWithAtLeastOneMatchingTag()
    {
        NotableDateService service = BuildService(
            FixedWithTags("Federal Tag", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Federal")),
            FixedWithTags("Regional Tag", 3, 15, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Regional")),
            FixedWithTags("Unrelated Tag", 6, 1, NotableDateCategory.Observance, nonWorking: false, ImmutableHashSet.Create("Christian")));

        var filter = NotableDateFilter.WithAnyTag("Federal", "Regional");
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(d => d.Tags.Contains("Federal") || d.Tags.Contains("Regional")));
    }

    // --------------------------------------------------------------------------------------
    // ForAnyCategory integration — data-driven
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> returns dates belonging to any of the
    /// supplied categories and excludes all others.
    /// </summary>
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
    // InDateRange standalone integration
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a standalone <see cref="NotableDateFilter.InDateRange" /> filter applied to a year
    /// query returns only dates whose span intersects the supplied range.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndStandaloneInDateRangeFilter_ShouldReturnOnlyDatesInRange()
    {
        NotableDateService service = BuildService(
            Fixed("Jan Holiday", 1, 1, NotableDateCategory.Holiday),
            Fixed("Jun Holiday", 6, 15, NotableDateCategory.Holiday),
            Fixed("Dec Holiday", 12, 25, NotableDateCategory.Holiday));

        var filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Jun Holiday", results[0].Name);
    }

    /// <summary>
    /// Verifies that combining <see cref="NotableDateFilter.InDateRange" /> with a category filter via
    /// <see cref="NotableDateFilter.And" /> returns only dates satisfying both constraints.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WithYearAndInDateRangeAndCategoryFilter_ShouldReturnIntersection()
    {
        NotableDateService service = BuildService(
            Fixed("Jan Holiday", 1, 1, NotableDateCategory.Holiday),
            Fixed("Jun Holiday", 6, 15, NotableDateCategory.Holiday),
            Fixed("Jun Observance", 6, 20, NotableDateCategory.Observance));

        NotableDateFilter filter = NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30))
            .And(NotableDateFilter.ForCategory(NotableDateCategory.Holiday));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Jun Holiday", results[0].Name);
    }

    // --------------------------------------------------------------------------------------
    // XML-driven integration — known static XML fragments from NotableDateRuleParserTests
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> correctly filters dates produced from rules
    /// parsed from a known static XML fragment, confirming that tags authored in XML are honoured by the
    /// filter pipeline.
    /// </summary>
    [DataRow("Public", true)]
    [DataRow("Civic", true)]
    [DataRow("PUBLIC", true)]
    [DataRow("civic", true)]
    [DataRow("Regional", false)]
    [DataRow("Religious", false)]
    [TestMethod]
    public void GetNotableDates_UsingParsedFixedRuleXml_WithTagFilter_ShouldMatchExpected(string tag, bool expectsDate)
    {
        // FixedRuleXml defines one Holiday rule (Fixed Jan 1, tags=[Public,Civic], territory=AU-NSW).
        // occurrenceYears=4 with firstYear=2000 means 2024 is an applicable year.
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
        // MultiRuleXml contains: New Year's Day (Fixed, Holiday), Easter Sunday (Algorithm, Observance),
        // Good Friday (OffsetFromAnchor, Holiday — requires Easter), Anzac Day (Fixed, Remembrance, territory=AU,NZ).
        // Without an algorithm registry, Easter Sunday and Good Friday resolve to nothing and are dropped.
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
    /// XML fragment correctly excludes dates that match only a subset of the required tags.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_UsingParsedFixedRuleXml_WithAllTagsFilter_WhenBothTagsPresent_ShouldReturnDate()
    {
        // FixedRuleXml tags are [Public, Civic]; requiring both must still return the date.
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
