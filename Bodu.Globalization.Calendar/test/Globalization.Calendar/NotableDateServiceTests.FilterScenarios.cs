// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.FilterScenarios.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Consolidates the year-and-filter test surface into a single <c>[TestMethod]</c> driven by the
/// <see cref="FilterScenarioKnownAnswer" /> KAT record. Each row encodes (service fixture, filter,
/// expected name set) — the shape that the majority of filter integration tests share.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that <c>GetNotableDates(year, filter)</c> returns occurrences whose name set matches the
    /// expectation encoded in the supplied known-answer row.
    /// </summary>
    /// <param name="knownAnswer">The filter scenario row supplied by <see cref="FilterScenarios" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(FilterScenarios),
        DynamicDataDisplayName = nameof(FilterScenarioAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(FilterScenarioAssertions))]
    public void GetNotableDates_WhenAppliedToFilterScenario_ShouldReturnExpectedNames(FilterScenarioKnownAnswer knownAnswer) =>
        FilterScenarioAssertions.AssertResultNamesMatch(knownAnswer);

    /// <summary>
    /// Enumerates the year-and-filter scenarios. Each row defines its own service fixture so the rule set
    /// stays self-contained per row; the shared assertion runs <c>GetNotableDates(year, filter)</c> and
    /// compares the returned name set against <see cref="FilterScenarioKnownAnswer.ExpectedNames" /> with
    /// <c>CollectionAssert.AreEquivalent</c>.
    /// </summary>
    /// <returns>A sequence of single-element object arrays whose only entry is a
    /// <see cref="FilterScenarioKnownAnswer" />.</returns>
    public static IEnumerable<object[]> FilterScenarios()
    {
        // Category filter — single category survives.
        yield return Scenario(
            "CategoryFilter_HolidayOnly",
            () => BuildService(
                Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
                Fixed("Observance B", 3, 15, NotableDateCategory.Observance),
                Fixed("Holiday C", 12, 25, NotableDateCategory.Holiday)),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            "Holiday A", "Holiday C");

        // Category filter — no rules match the selected category.
        yield return Scenario(
            "CategoryFilter_NoMatches",
            () => BuildService(
                Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
                Fixed("Holiday B", 12, 25, NotableDateCategory.Holiday)),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Cultural));

        // OR-of-two-categories — union.
        yield return Scenario(
            "OrCategoryFilter_Union",
            () => BuildService(
                Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
                Fixed("Observance B", 3, 15, NotableDateCategory.Observance),
                Fixed("Cultural C", 6, 1, NotableDateCategory.Cultural)),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
                .Or(NotableDateFilter.ForCategory(NotableDateCategory.Observance)),
            "Holiday A", "Observance B");

        // AND of category + non-working — intersection.
        yield return Scenario(
            "AndFilter_HolidayNonWorking",
            () => BuildService(
                FixedWithTags("Holiday Public", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ["Public"]),
                FixedWithTags("Holiday Private", 3, 15, NotableDateCategory.Holiday, nonWorking: false, ["Regional"]),
                Fixed("Observance", 6, 1, NotableDateCategory.Observance, nonWorking: true)),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Holiday).And(NotableDateFilter.IsNonWorkingDay()),
            "Holiday Public");

        // Tag filter — single tag.
        yield return Scenario(
            "TagFilter_PublicOnly",
            () => BuildService(
                FixedWithTags("Public Holiday", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ["Public"]),
                FixedWithTags("Regional Holiday", 6, 1, NotableDateCategory.Holiday, nonWorking: true, ["Regional"])),
            () => NotableDateFilter.WithTag("Public"),
            "Public Holiday");

        // Name filter — explicit allow-list of holiday names.
        yield return Scenario(
            "AnyNameFilter_NyeAndChristmas",
            () => BuildService(
                Fixed("New Year's Day", 1, 1),
                Fixed("Easter Sunday", 4, 1),
                Fixed("Christmas Day", 12, 25)),
            () => NotableDateFilter.WithAnyName("New Year's Day", "Christmas Day"),
            "New Year's Day", "Christmas Day");

        // Min-duration filter — only multi-day spans.
        yield return Scenario(
            "MinDurationFilter_AtLeastSeven",
            () => BuildService(
                Fixed("Single Day", 1, 1, NotableDateCategory.Holiday),
                Fixed("Festival", 6, 1, NotableDateCategory.Cultural) with { DurationDays = 7 },
                Fixed("Long Event", 9, 1, NotableDateCategory.Observance) with { DurationDays = 14 }),
            () => NotableDateFilter.WithMinDuration(7),
            "Festival", "Long Event");

        // Min-duration filter — minimum of one returns all dates.
        yield return Scenario(
            "MinDurationFilter_AtLeastOne",
            () => BuildService(
                Fixed("Day A", 1, 1, NotableDateCategory.Holiday),
                Fixed("Day B", 6, 1, NotableDateCategory.Observance) with { DurationDays = 3 }),
            () => NotableDateFilter.WithMinDuration(1),
            "Day A", "Day B");

        // AllOf — three-way intersection.
        yield return Scenario(
            "AllOfFilter_HolidayNonWorkingPublic",
            () => BuildService(
                FixedWithTags("Public Holiday", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ["Public"]),
                FixedWithTags("Private Holiday", 3, 15, NotableDateCategory.Holiday, nonWorking: false, ["Regional"]),
                FixedWithTags("Observance", 6, 1, NotableDateCategory.Observance, nonWorking: true, ["Public"])),
            () => NotableDateFilter.AllOf(
                NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
                NotableDateFilter.IsNonWorkingDay(),
                NotableDateFilter.WithTag("Public")),
            "Public Holiday");

        // AnyOf — union over two categories.
        yield return Scenario(
            "AnyOfFilter_HolidayOrSeasonal",
            () => BuildService(
                Fixed("Holiday", 1, 1, NotableDateCategory.Holiday),
                Fixed("Observance", 3, 15, NotableDateCategory.Observance),
                Fixed("Cultural", 6, 1, NotableDateCategory.Cultural),
                Fixed("Seasonal", 9, 22, NotableDateCategory.Seasonal)),
            () => NotableDateFilter.AnyOf(
                NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
                NotableDateFilter.ForCategory(NotableDateCategory.Seasonal)),
            "Holiday", "Seasonal");

        // WithAllTags — requires every tag.
        yield return Scenario(
            "WithAllTagsFilter_PublicAndFederal",
            () => BuildService(
                FixedWithTags("Both Tags", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ["Public", "Federal"]),
                FixedWithTags("One Tag Only", 6, 1, NotableDateCategory.Holiday, nonWorking: true, ["Public"]),
                FixedWithTags("No Tags", 12, 25, NotableDateCategory.Holiday, nonWorking: true, [])),
            () => NotableDateFilter.WithAllTags("Public", "Federal"),
            "Both Tags");

        // WithAnyTag — requires at least one tag.
        yield return Scenario(
            "WithAnyTagFilter_FederalOrRegional",
            () => BuildService(
                FixedWithTags("Federal Tag", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ["Federal"]),
                FixedWithTags("Regional Tag", 3, 15, NotableDateCategory.Holiday, nonWorking: true, ["Regional"]),
                FixedWithTags("Unrelated Tag", 6, 1, NotableDateCategory.Observance, nonWorking: false, ["Christian"])),
            () => NotableDateFilter.WithAnyTag("Federal", "Regional"),
            "Federal Tag", "Regional Tag");

        // InDateRange standalone — filters by anchor date intersecting the range.
        yield return Scenario(
            "InDateRangeFilter_JuneOnly",
            () => BuildService(
                Fixed("Jan Holiday", 1, 1, NotableDateCategory.Holiday),
                Fixed("Jun Holiday", 6, 15, NotableDateCategory.Holiday),
                Fixed("Dec Holiday", 12, 25, NotableDateCategory.Holiday)),
            () => NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30)),
            "Jun Holiday");

        // InDateRange AND category — both constraints must hold.
        yield return Scenario(
            "InDateRangeAndCategoryFilter_JuneHolidaysOnly",
            () => BuildService(
                Fixed("Jun Holiday", 6, 15, NotableDateCategory.Holiday),
                Fixed("Jun Observance", 6, 20, NotableDateCategory.Observance),
                Fixed("Dec Holiday", 12, 25, NotableDateCategory.Holiday)),
            () => NotableDateFilter.InDateRange(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30))
                .And(NotableDateFilter.ForCategory(NotableDateCategory.Holiday)),
            "Jun Holiday");

        // -------------------------------------------------------------------------------------------------
        // Range-API overload — uses GetNotableDates(startDate, endDate, filter) via the Query delegate.
        // -------------------------------------------------------------------------------------------------

        yield return ScenarioWithQuery(
            "DateRangeAndCategoryFilter_FirstHalfHoliday",
            () => BuildService(
                Fixed("Holiday Jan", 1, 1, NotableDateCategory.Holiday),
                Fixed("Observance Mar", 3, 15, NotableDateCategory.Observance),
                Fixed("Holiday Dec", 12, 25, NotableDateCategory.Holiday)),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            (service, filter) => service.GetNotableDates(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30), filter),
            "Holiday Jan");

        // -------------------------------------------------------------------------------------------------
        // Single-date overload — uses GetNotableDates(date, filter) via the Query delegate.
        // -------------------------------------------------------------------------------------------------

        yield return ScenarioWithQuery(
            "SingleDateAndMatchingFilter",
            () => BuildService(
                Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
                Fixed("Observance B", 1, 1, NotableDateCategory.Observance)),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            (service, filter) => service.GetNotableDates(new DateTime(2024, 1, 1), filter),
            "Holiday A");

        yield return ScenarioWithQuery(
            "SingleDateAndNonMatchingFilter",
            () => BuildService(Fixed("Observance B", 1, 1, NotableDateCategory.Observance)),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            (service, filter) => service.GetNotableDates(new DateTime(2024, 1, 1), filter));

        // -------------------------------------------------------------------------------------------------
        // ForAnyCategory — folds the four [DataRow]s of the bespoke count-only test.
        // Fixture: Holiday / Observance / Cultural / Seasonal.
        // -------------------------------------------------------------------------------------------------

        Func<NotableDateService> fourCategoryFixture = () => BuildService(
            Fixed("Holiday", 1, 1, NotableDateCategory.Holiday),
            Fixed("Observance", 3, 15, NotableDateCategory.Observance),
            Fixed("Cultural", 6, 1, NotableDateCategory.Cultural),
            Fixed("Seasonal", 9, 22, NotableDateCategory.Seasonal));

        yield return Scenario(
            "ForAnyCategory_HolidayOrObservance",
            fourCategoryFixture,
            () => NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday, NotableDateCategory.Observance),
            "Holiday", "Observance");

        yield return Scenario(
            "ForAnyCategory_HolidayOrCultural",
            fourCategoryFixture,
            () => NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday, NotableDateCategory.Cultural),
            "Holiday", "Cultural");

        yield return Scenario(
            "ForAnyCategory_ObservanceOrSeasonal",
            fourCategoryFixture,
            () => NotableDateFilter.ForAnyCategory(NotableDateCategory.Observance, NotableDateCategory.Seasonal),
            "Observance", "Seasonal");

        yield return Scenario(
            "ForAnyCategory_RemembranceOrOther_NoMatches",
            fourCategoryFixture,
            () => NotableDateFilter.ForAnyCategory(NotableDateCategory.Remembrance, NotableDateCategory.Other));

        // -------------------------------------------------------------------------------------------------
        // XML-driven scenarios — service built from rule fragments shared with NotableDateRuleParserTests.
        // The Query delegate threads territoryCode through the year overload where needed.
        // -------------------------------------------------------------------------------------------------

        Func<NotableDateService> fixedRuleXmlFixture = () => BuildServiceFromXml(NotableDateRuleParserTests.FixedRuleXml);

        // WithTag — folds the six [DataRow]s of the bespoke tag-filter sweep over FixedRuleXml.
        // FixedRuleXml's single rule is named "Fixed Rule Test" with tags [Public, Civic] (case-insensitive).
        yield return XmlTagRow("XmlTagFilter_Public", fixedRuleXmlFixture, "Public", "Fixed Rule Test");
        yield return XmlTagRow("XmlTagFilter_Civic", fixedRuleXmlFixture, "Civic", "Fixed Rule Test");
        yield return XmlTagRow("XmlTagFilter_PUBLIC_CaseInsensitive", fixedRuleXmlFixture, "PUBLIC", "Fixed Rule Test");
        yield return XmlTagRow("XmlTagFilter_civic_CaseInsensitive", fixedRuleXmlFixture, "civic", "Fixed Rule Test");
        yield return XmlTagRow("XmlTagFilter_Regional_NoMatch", fixedRuleXmlFixture, "Regional");
        yield return XmlTagRow("XmlTagFilter_Religious_NoMatch", fixedRuleXmlFixture, "Religious");

        // WithName — single-name filter against the FixedRuleXml fixture.
        yield return ScenarioWithQuery(
            "XmlNameFilter_FixedRuleTest",
            fixedRuleXmlFixture,
            () => NotableDateFilter.WithName("Fixed Rule Test"),
            (service, filter) => service.GetNotableDates(2024, filter, territoryCode: "AU-NSW"),
            "Fixed Rule Test");

        // WithAllTags — both tags present.
        yield return ScenarioWithQuery(
            "XmlAllTagsFilter_PublicAndCivic_BothPresent",
            fixedRuleXmlFixture,
            () => NotableDateFilter.WithAllTags("Public", "Civic"),
            (service, filter) => service.GetNotableDates(2024, filter, territoryCode: "AU-NSW"),
            "Fixed Rule Test");

        // WithAllTags — one required tag absent.
        yield return ScenarioWithQuery(
            "XmlAllTagsFilter_PublicAndReligious_OneAbsent",
            fixedRuleXmlFixture,
            () => NotableDateFilter.WithAllTags("Public", "Religious"),
            (service, filter) => service.GetNotableDates(2024, filter, territoryCode: "AU-NSW"));

        // MultiRuleXml Holiday category — New Year's Day is the only resolvable Holiday because the
        // Algorithm-strategy Easter Sunday rule fails without a registered algorithm and Good Friday
        // depends on Easter; Anzac Day is Remembrance, not Holiday.
        yield return Scenario(
            "XmlMultiRuleHolidayCategory_OnlyNewYearsDayResolves",
            () => BuildServiceFromXml(NotableDateRuleParserTests.MultiRuleXml),
            () => NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
            "New Year's Day");
    }

    /// <summary>
    /// Builds an XML-driven WithTag scenario row that threads the AU-NSW territoryCode through the year
    /// overload. Used by the six tag-filter rows that exercise the FixedRuleXml fixture.
    /// </summary>
    /// <param name="scenario">Row label.</param>
    /// <param name="serviceFactory">Factory returning the XML-built service.</param>
    /// <param name="tag">The tag passed to <see cref="NotableDateFilter.WithTag" />.</param>
    /// <param name="expectedNames">Names the filter should yield (empty for no-match cases).</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] XmlTagRow(
        string scenario,
        Func<NotableDateService> serviceFactory,
        string tag,
        params string[] expectedNames) =>
        ScenarioWithQuery(
            scenario,
            serviceFactory,
            () => NotableDateFilter.WithTag(tag),
            (service, filter) => service.GetNotableDates(2024, filter, territoryCode: "AU-NSW"),
            expectedNames);

    /// <summary>
    /// Wraps a filter scenario in the single-element object array shape
    /// <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> expects. Uses the
    /// default <c>GetNotableDates(year, filter)</c> overload at year <c>2024</c>.
    /// </summary>
    /// <param name="scenario">Short scenario label rendered in the row's display name.</param>
    /// <param name="serviceFactory">Factory that constructs the fixture service.</param>
    /// <param name="filterFactory">Factory that constructs the filter under test.</param>
    /// <param name="expectedNames">The expected holiday name set.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] Scenario(
        string scenario,
        Func<NotableDateService> serviceFactory,
        Func<NotableDateFilter> filterFactory,
        params string[] expectedNames) =>
        [
            new FilterScenarioKnownAnswer
            {
                Scenario = scenario,
                ServiceFactory = serviceFactory,
                FilterFactory = filterFactory,
                ExpectedNames = expectedNames,
            },
        ];

    /// <summary>
    /// Same as <see cref="Scenario" />, but routes the row through a caller-supplied <paramref name="query" />
    /// delegate. Used by rows that exercise a non-default <c>GetNotableDates</c> overload (range, single
    /// date, with territoryCode).
    /// </summary>
    /// <param name="scenario">Short scenario label rendered in the row's display name.</param>
    /// <param name="serviceFactory">Factory that constructs the fixture service.</param>
    /// <param name="filterFactory">Factory that constructs the filter under test.</param>
    /// <param name="query">Delegate invoking the desired <c>GetNotableDates</c> overload.</param>
    /// <param name="expectedNames">The expected holiday name set.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] ScenarioWithQuery(
        string scenario,
        Func<NotableDateService> serviceFactory,
        Func<NotableDateFilter> filterFactory,
        Func<NotableDateService, NotableDateFilter, IReadOnlyList<NotableDate>> query,
        params string[] expectedNames) =>
        [
            new FilterScenarioKnownAnswer
            {
                Scenario = scenario,
                ServiceFactory = serviceFactory,
                FilterFactory = filterFactory,
                Query = query,
                ExpectedNames = expectedNames,
            },
        ];
}
