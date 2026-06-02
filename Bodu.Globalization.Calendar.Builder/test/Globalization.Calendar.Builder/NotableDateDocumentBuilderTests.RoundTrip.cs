// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;
using Bodu.Extensions;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    // ============================================================================
    // Strategy: Fixed
    // ============================================================================

    /// <summary>
    /// Verifies that a Fixed strategy authored with a numeric Gregorian month round-trips Build → ToXml → ParseXml
    /// with every strategy field preserved.
    /// </summary>
    [TestMethod]
    public void RoundTrip_FixedNumericMonth_ShouldPreserveAllStrategyFields()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(12, 25))));

        Assert.HasCount(1, parsed);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual("Christmas Day", rule.Name);
        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.AreEqual(12, rule.Month);
        Assert.AreEqual(25, rule.Day);
        Assert.IsNull(rule.CalendarMonthAlias);
        Assert.IsFalse(rule.SkipLeapMonth);
        Assert.IsFalse(rule.SweepCalendarYears);
    }

    /// <summary>
    /// Verifies that a Fixed strategy authored with an English month name string round-trips with the month
    /// resolved to its numeric value.
    /// </summary>
    [TestMethod]
    public void RoundTrip_FixedEnglishMonthName_ShouldResolveToNumericMonth()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("St Patrick's Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Cultural)
                    .Fixed("March", 17))));

        Assert.AreEqual(3, parsed[0].Month);
        Assert.AreEqual(17, parsed[0].Day);
        Assert.IsNull(parsed[0].CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a Fixed strategy authored with a Hebrew fixed-position month token (Tishri…AdarII) round-trips
    /// as a numeric month 1–7 with no calendar-month alias.
    /// </summary>
    [TestMethod]
    [DataRow("Tishri", 1)]
    [DataRow("Heshvan", 2)]
    [DataRow("Kislev", 3)]
    [DataRow("Tevet", 4)]
    [DataRow("Shevat", 5)]
    [DataRow("AdarI", 6)]
    [DataRow("AdarII", 7)]
    public void RoundTrip_FixedHebrewFixedMonth_ShouldResolveToNumericMonth(string monthToken, int expectedMonth)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .CalendarType(typeof(HebrewCalendar))
                    .Fixed(monthToken, 1))));

        Assert.AreEqual(expectedMonth, parsed[0].Month);
        Assert.IsNull(parsed[0].CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a Fixed strategy authored with a leap-year-dependent Hebrew month alias round-trips with the
    /// alias stored on <see cref="NotableDateRule.CalendarMonthAlias" /> and the numeric month left
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    [DataRow("LastAdar")]
    [DataRow("Nisan")]
    [DataRow("Iyar")]
    [DataRow("Sivan")]
    [DataRow("Tammuz")]
    [DataRow("Av")]
    [DataRow("Elul")]
    public void RoundTrip_FixedHebrewAliasMonth_ShouldPreserveAlias(string alias)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .CalendarType(typeof(HebrewCalendar))
                    .Fixed(alias, 15))));

        Assert.IsNull(parsed[0].Month);
        Assert.AreEqual(alias, parsed[0].CalendarMonthAlias);
        Assert.AreEqual(15, parsed[0].Day);
    }

    /// <summary>
    /// Verifies that the lunisolar boolean flags <c>skipLeapMonth</c> and <c>sweepCalendarYears</c> round-trip
    /// through XML serialisation when both are <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void RoundTrip_FixedWithLunisolarFlagsTrue_ShouldPreserveBothFlags()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .CalendarType(typeof(HebrewCalendar))
                    .Fixed(1, 1, skipLeapMonth: true, sweepCalendarYears: true))));

        Assert.IsTrue(parsed[0].SkipLeapMonth);
        Assert.IsTrue(parsed[0].SweepCalendarYears);
    }

    /// <summary>
    /// Verifies that the lunisolar boolean flags default to <see langword="false" /> on the parsed rule when
    /// neither was set on the builder.
    /// </summary>
    [TestMethod]
    public void RoundTrip_FixedWithDefaultLunisolarFlags_ShouldYieldBothFalse()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1))));

        Assert.IsFalse(parsed[0].SkipLeapMonth);
        Assert.IsFalse(parsed[0].SweepCalendarYears);
    }

    // ============================================================================
    // Strategy: DayOfWeekInMonth
    // ============================================================================

    /// <summary>
    /// Verifies that each <see cref="WeekOfMonthOrdinal" /> value round-trips intact when paired with a
    /// <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> strategy.
    /// </summary>
    [TestMethod]
    [DataRow(WeekOfMonthOrdinal.First)]
    [DataRow(WeekOfMonthOrdinal.Second)]
    [DataRow(WeekOfMonthOrdinal.Third)]
    [DataRow(WeekOfMonthOrdinal.Fourth)]
    [DataRow(WeekOfMonthOrdinal.Fifth)]
    [DataRow(WeekOfMonthOrdinal.Last)]
    public void RoundTrip_DayOfWeekInMonthAllOrdinals_ShouldPreserveOrdinal(WeekOfMonthOrdinal ordinal)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .DayOfWeekInMonth(6, DayOfWeek.Monday, ordinal))));

        Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, parsed[0].Strategy);
        Assert.AreEqual(6, parsed[0].Month);
        Assert.AreEqual(DayOfWeek.Monday, parsed[0].DayOfWeek);
        Assert.AreEqual(ordinal, parsed[0].WeekOrdinal);
    }

    /// <summary>
    /// Verifies that each <see cref="DayOfWeek" /> value round-trips intact when paired with a
    /// <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> strategy.
    /// </summary>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    [DataRow(DayOfWeek.Friday)]
    [DataRow(DayOfWeek.Saturday)]
    public void RoundTrip_DayOfWeekInMonthAllDays_ShouldPreserveDay(DayOfWeek day)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .DayOfWeekInMonth(3, day, WeekOfMonthOrdinal.Second))));

        Assert.AreEqual(day, parsed[0].DayOfWeek);
    }

    // ============================================================================
    // Strategy: OffsetFromAnchor
    // ============================================================================

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> strategy preserves a positive offset
    /// through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_OffsetFromAnchorPositive_ShouldPreserveOffset()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Easter Monday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .OffsetFromAnchor("Easter Sunday", 1))));

        Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, parsed[0].Strategy);
        Assert.AreEqual("Easter Sunday", parsed[0].AnchorRuleName);
        Assert.AreEqual(1, parsed[0].OffsetDays);
    }

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> strategy preserves a negative offset
    /// through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_OffsetFromAnchorNegative_ShouldPreserveOffset()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Good Friday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .OffsetFromAnchor("Easter Sunday", -2))));

        Assert.AreEqual(-2, parsed[0].OffsetDays);
    }

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> strategy preserves a zero offset
    /// through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_OffsetFromAnchorZero_ShouldPreserveZeroOffset()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Aliased Easter", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .OffsetFromAnchor("Easter Sunday", 0))));

        Assert.AreEqual(0, parsed[0].OffsetDays);
    }

    // ============================================================================
    // Strategy: WeekdayNearDate
    // ============================================================================

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy with the
    /// <see cref="WeekdayProximity.OnOrAfter" /> direction (Nordic Midsummer Day pattern) round-trips with every
    /// strategy field preserved.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WeekdayNearDateOnOrAfter_ShouldPreserveAllStrategyFields()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Midsummer Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .WeekdayNearDate(month: 6, day: 20, DayOfWeek.Saturday, WeekdayProximity.OnOrAfter))));

        Assert.HasCount(1, parsed);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual(DateResolutionStrategy.WeekdayNearDate, rule.Strategy);
        Assert.AreEqual(6, rule.Month);
        Assert.AreEqual(20, rule.Day);
        Assert.AreEqual(DayOfWeek.Saturday, rule.DayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy with the
    /// <see cref="WeekdayProximity.OnOrBefore" /> direction (German Buß- und Bettag pattern) round-trips with the
    /// direction preserved.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WeekdayNearDateOnOrBefore_ShouldPreserveDirection()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Repentance Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .WeekdayNearDate(month: 11, day: 22, DayOfWeek.Wednesday, WeekdayProximity.OnOrBefore))));

        Assert.AreEqual(WeekdayProximity.OnOrBefore, parsed[0].WeekdayProximity);
        Assert.AreEqual(DayOfWeek.Wednesday, parsed[0].DayOfWeek);
        Assert.AreEqual(11, parsed[0].Month);
        Assert.AreEqual(22, parsed[0].Day);
    }

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy with the
    /// <see cref="WeekdayProximity.Nearest" /> direction round-trips with the direction preserved.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WeekdayNearDateNearest_ShouldPreserveDirection()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Observed Holiday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .WeekdayNearDate(month: 7, day: 4, DayOfWeek.Monday, WeekdayProximity.Nearest))));

        Assert.AreEqual(WeekdayProximity.Nearest, parsed[0].WeekdayProximity);
    }

    // ============================================================================
    // Strategy: RelativeWeekdayInMonth
    // ============================================================================

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" /> strategy (US Election Day
    /// pattern) round-trips with every strategy field preserved.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RelativeWeekdayInMonthElectionDay_ShouldPreserveAllStrategyFields()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Election Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .RelativeWeekdayInMonth(
                        month: 11,
                        DayOfWeek.Monday,
                        WeekOfMonthOrdinal.First,
                        DayOfWeek.Tuesday,
                        WeekdayProximity.OnOrAfter))));

        Assert.HasCount(1, parsed);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual(DateResolutionStrategy.RelativeWeekdayInMonth, rule.Strategy);
        Assert.AreEqual(11, rule.Month);
        Assert.AreEqual(DayOfWeek.Monday, rule.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.First, rule.WeekOrdinal);
        Assert.AreEqual(DayOfWeek.Tuesday, rule.RelativeDayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" /> strategy with the
    /// <see cref="WeekdayProximity.OnOrBefore" /> direction round-trips with the direction preserved.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RelativeWeekdayInMonthOnOrBefore_ShouldPreserveDirection()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Sample Date", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .RelativeWeekdayInMonth(
                        month: 10,
                        DayOfWeek.Friday,
                        WeekOfMonthOrdinal.Last,
                        DayOfWeek.Thursday,
                        WeekdayProximity.OnOrBefore))));

        Assert.AreEqual(WeekdayProximity.OnOrBefore, parsed[0].WeekdayProximity);
        Assert.AreEqual(WeekOfMonthOrdinal.Last, parsed[0].WeekOrdinal);
    }

    // ============================================================================
    // Strategy: Algorithm
    // ============================================================================

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.Algorithm" /> strategy with only a registry key preserves
    /// the key and leaves the type, month, and day fields <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AlgorithmKeyOnly_ShouldPreserveKey()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Easter Sunday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Algorithm(key: "easter-gregorian"))));

        Assert.AreEqual(DateResolutionStrategy.Algorithm, parsed[0].Strategy);
        Assert.AreEqual("easter-gregorian", parsed[0].AlgorithmKey);
        Assert.IsNull(parsed[0].AlgorithmType);
        Assert.IsNull(parsed[0].AlgorithmMonth);
        Assert.IsNull(parsed[0].AlgorithmDay);
    }

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.Algorithm" /> strategy with a CLR algorithm type preserves
    /// the resolved <see cref="Type" /> through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AlgorithmTypeOnly_ShouldPreserveType()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Easter Sunday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Algorithm(algorithmType: typeof(EasterSundayNotableDateAlgorithm)))));

        Assert.AreEqual(DateResolutionStrategy.Algorithm, parsed[0].Strategy);
        Assert.AreEqual(typeof(EasterSundayNotableDateAlgorithm), parsed[0].AlgorithmType);
    }

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.Algorithm" /> strategy preserves the optional
    /// month and day constructor arguments through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AlgorithmWithMonthAndDay_ShouldPreserveConstructorArguments()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Custom Hebrew Festival", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Algorithm(key: "hebrew-festival", month: "Nisan", day: 14))));

        Assert.AreEqual("hebrew-festival", parsed[0].AlgorithmKey);
        Assert.AreEqual("Nisan", parsed[0].AlgorithmMonth);
        Assert.AreEqual(14, parsed[0].AlgorithmDay);
    }

    // ============================================================================
    // Rule-level scopes and metadata
    // ============================================================================

    /// <summary>
    /// Verifies that each <see cref="NotableDateCategory" /> value round-trips intact on the rule.
    /// </summary>
    [TestMethod]
    [DataRow(NotableDateCategory.Holiday)]
    [DataRow(NotableDateCategory.Observance)]
    [DataRow(NotableDateCategory.Remembrance)]
    [DataRow(NotableDateCategory.Cultural)]
    [DataRow(NotableDateCategory.Religious)]
    [DataRow(NotableDateCategory.Seasonal)]
    [DataRow(NotableDateCategory.Civic)]
    [DataRow(NotableDateCategory.Bank)]
    [DataRow(NotableDateCategory.School)]
    [DataRow(NotableDateCategory.Regional)]
    [DataRow(NotableDateCategory.Other)]
    public void RoundTrip_RuleWithEachCategory_ShouldPreserveCategory(NotableDateCategory category)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule.Category(category).Fixed(1, 1))));

        Assert.AreEqual(category, parsed[0].Category);
    }

    /// <summary>
    /// Verifies that a comma-separated territory list round-trips intact on the rule.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithMultiTerritory_ShouldPreserveTerritoryList()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU-NSW,AU-VIC,AU-QLD")
                    .Fixed(1, 1))));

        Assert.AreEqual("AU-NSW,AU-VIC,AU-QLD", parsed[0].TerritoryCode);
    }

    /// <summary>
    /// Verifies that a calendar type set on the rule round-trips to the same <see cref="Type" /> after parsing.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithCalendarType_ShouldPreserveCalendarType()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Rosh Hashanah", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .CalendarType(typeof(HebrewCalendar))
                    .Fixed("Tishri", 1))));

        Assert.AreEqual(typeof(HebrewCalendar), parsed[0].CalendarType);
    }

    /// <summary>
    /// Verifies that the recurrence cadence set via <see cref="NotableDateRuleBuilder.OccurrenceYears" /> round-trips
    /// intact on the rule.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithOccurrenceYears_ShouldPreserveCadence()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Summer Olympics", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Cultural)
                    .FirstYear(1896)
                    .OccurrenceYears(4)
                    .Fixed(7, 15))));

        Assert.AreEqual(1896, parsed[0].FirstYear);
        Assert.AreEqual(4, parsed[0].OccurrenceYears);
    }

    /// <summary>
    /// Verifies that a multi-day duration on the rule round-trips intact.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithDurationDays_ShouldPreserveDuration()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Hanukkah", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Duration(8)
                    .CalendarType(typeof(HebrewCalendar))
                    .Fixed("Kislev", 25))));

        Assert.AreEqual(8, parsed[0].DurationDays);
    }

    /// <summary>
    /// Verifies that an explicit <see cref="NotableDateRuleBuilder.RuleName" /> round-trips intact on the parsed rule.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithExplicitRuleName_ShouldPreserveRuleName()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("King's Birthday", date => date
                .AddRule(rule => rule
                    .RuleName("queensland-rule")
                    .Category(NotableDateCategory.Holiday)
                    .DayOfWeekInMonth(10, DayOfWeek.Monday, WeekOfMonthOrdinal.First))));

        Assert.AreEqual("queensland-rule", parsed[0].RuleName);
    }

    /// <summary>
    /// Verifies that an unset <see cref="NotableDateRuleBuilder.RuleName" /> still produces a non-empty
    /// <see cref="NotableDateRule.RuleName" /> after the round-trip, because the builder auto-generates a
    /// strategy-suffixed name to satisfy the XSD-required <c>name</c> attribute.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithoutExplicitRuleName_ShouldGenerateStrategySuffixedName()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 25))));

        Assert.AreEqual("Christmas Day (Fixed)", parsed[0].RuleName);
    }

    /// <summary>
    /// Verifies that multiple tags added in order round-trip as a set on the rule.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithMultipleTags_ShouldPreserveAllTags()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Fixed(12, 25)
                    .AddTag("Christian")
                    .AddTag("Public")
                    .AddTag("Federal"))));

        Assert.HasCount(3, parsed[0].Tags);
        Assert.Contains("Christian", parsed[0].Tags);
        Assert.Contains("Public", parsed[0].Tags);
        Assert.Contains("Federal", parsed[0].Tags);
    }

    /// <summary>
    /// Verifies that an explicit <see cref="NotableDateRuleBuilder.NonWorking(bool)" /> with <see langword="false" />
    /// round-trips as <see langword="false" /> on the parsed rule rather than reverting to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithNonWorkingFalse_ShouldPreserveExplicitFalse()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Observance)
                    .NonWorking(false)
                    .Fixed(1, 1))));

        Assert.IsFalse(parsed[0].IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that an unset <see cref="NotableDateRuleBuilder.NonWorking" /> round-trips as <see langword="null" />
    /// on the parsed rule.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithoutNonWorking_ShouldLeaveIsNonWorkingDayNull()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Observance)
                    .Fixed(1, 1))));

        Assert.IsNull(parsed[0].IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that a rule with every optional scalar field set populates each on the parsed result.
    /// </summary>
    [TestMethod]
    public void RoundTrip_RuleWithAllOptionalScalars_ShouldPreserveEveryField()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Comprehensive Rule", date => date
                .AddRule(rule => rule
                    .RuleName("variant-a")
                    .Category(NotableDateCategory.Civic)
                    .NonWorking()
                    .FirstYear(1950)
                    .LastYear(2099)
                    .OccurrenceYears(2)
                    .Duration(5)
                    .Priority(25)
                    .Territory("AU-NSW")
                    .Comment("Round-trip coverage test")
                    .AddTag("Federal")
                    .AddTag("Public")
                    .Fixed(7, 4))));

        NotableDateRule rule = parsed[0];
        Assert.AreEqual("Comprehensive Rule", rule.Name);
        Assert.AreEqual("variant-a", rule.RuleName);
        Assert.AreEqual(NotableDateCategory.Civic, rule.Category);
        Assert.IsTrue(rule.IsNonWorkingDay);
        Assert.AreEqual(1950, rule.FirstYear);
        Assert.AreEqual(2099, rule.LastYear);
        Assert.AreEqual(2, rule.OccurrenceYears);
        Assert.AreEqual(5, rule.DurationDays);
        Assert.AreEqual(25, rule.Priority);
        Assert.AreEqual("AU-NSW", rule.TerritoryCode);
        Assert.AreEqual("Round-trip coverage test", rule.Comment);
        Assert.HasCount(2, rule.Tags);
    }

    // ============================================================================
    // Adjustments: triggers
    // ============================================================================

    /// <summary>
    /// Verifies that every <see cref="AdjustmentTrigger" /> value round-trips intact on an adjustment.
    /// </summary>
    [TestMethod]
    [DataRow(AdjustmentTrigger.Always)]
    [DataRow(AdjustmentTrigger.IfDayOfWeek)]
    [DataRow(AdjustmentTrigger.IfWeekend)]
    [DataRow(AdjustmentTrigger.IfWeekday)]
    [DataRow(AdjustmentTrigger.IfNonWorkingDay)]
    [DataRow(AdjustmentTrigger.IfBeforeFixedDate)]
    [DataRow(AdjustmentTrigger.IfAfterFixedDate)]
    [DataRow(AdjustmentTrigger.IfLeapYear)]
    [DataRow(AdjustmentTrigger.IfNthOccurrenceInMonth)]
    [DataRow(AdjustmentTrigger.Custom)]
    public void RoundTrip_AdjustmentAllTriggers_ShouldPreserveTrigger(AdjustmentTrigger trigger)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("a", adj => adj
                        .When(trigger)
                        .Action(AdjustmentAction.None)
                        .OnDayOfWeek(DayOfWeek.Sunday)
                        .ComparisonDate(3, 20)
                        .OrdinalOccurrence(WeekOfMonthOrdinal.Second)))));

        Assert.AreEqual(trigger, parsed[0].Adjustments[0].Trigger);
    }

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfDayOfWeek" /> trigger preserves the configured
    /// <see cref="DayOfWeek" /> through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentIfDayOfWeek_ShouldPreserveDayOfWeek()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("a", adj => adj
                        .When(AdjustmentTrigger.IfDayOfWeek)
                        .Action(AdjustmentAction.MoveToNextWeekday)
                        .OnDayOfWeek(DayOfWeek.Friday)))));

        Assert.AreEqual(DayOfWeek.Friday, parsed[0].Adjustments[0].DayOfWeek);
    }

    /// <summary>
    /// Verifies that an adjustment with <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> preserves the comparison
    /// month and day through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentIfBeforeFixedDate_ShouldPreserveComparisonDate()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Seasonal)
                    .Fixed(3, 1)
                    .AddAdjustment("pre-equinox", adj => adj
                        .When(AdjustmentTrigger.IfBeforeFixedDate)
                        .Action(AdjustmentAction.AddDays)
                        .ComparisonDate(3, 20)
                        .OffsetDays(1)))));

        ObservanceAdjustment adj = parsed[0].Adjustments[0];
        Assert.IsNotNull(adj.ComparisonDate);
        Assert.AreEqual(3, adj.ComparisonDate!.Value.Month);
        Assert.AreEqual(20, adj.ComparisonDate!.Value.Day);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> preserves the configured
    /// <see cref="WeekOfMonthOrdinal" /> through the round-trip.
    /// </summary>
    [TestMethod]
    [DataRow(WeekOfMonthOrdinal.First)]
    [DataRow(WeekOfMonthOrdinal.Second)]
    [DataRow(WeekOfMonthOrdinal.Third)]
    [DataRow(WeekOfMonthOrdinal.Fourth)]
    [DataRow(WeekOfMonthOrdinal.Fifth)]
    [DataRow(WeekOfMonthOrdinal.Last)]
    public void RoundTrip_AdjustmentIfNthOccurrenceInMonth_ShouldPreserveOrdinal(WeekOfMonthOrdinal ordinal)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("nth", adj => adj
                        .When(AdjustmentTrigger.IfNthOccurrenceInMonth)
                        .Action(AdjustmentAction.None)
                        .OnDayOfWeek(DayOfWeek.Monday)
                        .OrdinalOccurrence(ordinal)))));

        Assert.AreEqual(ordinal, parsed[0].Adjustments[0].WeekOrdinal);
    }

    // ============================================================================
    // Adjustments: actions
    // ============================================================================

    /// <summary>
    /// Verifies that every <see cref="AdjustmentAction" /> value round-trips intact on an adjustment.
    /// </summary>
    [TestMethod]
    [DataRow(AdjustmentAction.None)]
    [DataRow(AdjustmentAction.AddDays)]
    [DataRow(AdjustmentAction.MoveToNextWeekday)]
    [DataRow(AdjustmentAction.MoveToPreviousWeekday)]
    [DataRow(AdjustmentAction.MoveToNextWorkingDay)]
    [DataRow(AdjustmentAction.ReplaceWithNamedDate)]
    [DataRow(AdjustmentAction.Custom)]
    public void RoundTrip_AdjustmentAllActions_ShouldPreserveAction(AdjustmentAction action)
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("a", adj => adj
                        .When(AdjustmentTrigger.Always)
                        .Action(action)
                        .Target("Other Rule")
                        .OffsetDays(1)))));

        Assert.AreEqual(action, parsed[0].Adjustments[0].Action);
    }

    /// <summary>
    /// Verifies that an <see cref="AdjustmentAction.ReplaceWithNamedDate" /> action preserves its target rule name
    /// through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentReplaceWithNamedDate_ShouldPreserveTarget()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Anzac Day Observance", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Remembrance)
                    .Fixed(4, 25)
                    .AddAdjustment("sub", adj => adj
                        .When(AdjustmentTrigger.IfDayOfWeek)
                        .OnDayOfWeek(DayOfWeek.Sunday)
                        .Action(AdjustmentAction.ReplaceWithNamedDate)
                        .Target("Anzac Day (Substitute)")))));

        Assert.AreEqual("Anzac Day (Substitute)", parsed[0].Adjustments[0].TargetRuleName);
    }

    // ============================================================================
    // Adjustments: scopes and metadata
    // ============================================================================

    /// <summary>
    /// Verifies that an adjustment scoped by territory preserves the territory code through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentWithTerritoryScope_ShouldPreserveTerritory()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("scoped", adj => adj
                        .When(AdjustmentTrigger.IfWeekend)
                        .Action(AdjustmentAction.MoveToNextWeekday)
                        .Territory("AU-NSW,AU-VIC")))));

        Assert.AreEqual("AU-NSW,AU-VIC", parsed[0].Adjustments[0].TerritoryCode);
    }

    /// <summary>
    /// Verifies that an adjustment scoped by calendar type preserves the type through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentWithCalendarTypeScope_ShouldPreserveCalendarType()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Fixed(1, 1)
                    .AddAdjustment("hebrew-only", adj => adj
                        .When(AdjustmentTrigger.Always)
                        .Action(AdjustmentAction.None)
                        .CalendarType(typeof(HebrewCalendar))))));

        Assert.AreEqual(typeof(HebrewCalendar), parsed[0].Adjustments[0].CalendarType);
    }

    /// <summary>
    /// Verifies that an adjustment scoped by inclusive year bounds preserves both bounds through the round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentWithYearBounds_ShouldPreserveBothBounds()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("bounded", adj => adj
                        .When(AdjustmentTrigger.IfWeekend)
                        .Action(AdjustmentAction.MoveToNextWeekday)
                        .FromYear(2010)
                        .ToYear(2050)))));

        ObservanceAdjustment adj = parsed[0].Adjustments[0];
        Assert.AreEqual(2010, adj.EffectiveFromYear);
        Assert.AreEqual(2050, adj.EffectiveToYear);
    }

    /// <summary>
    /// Verifies that an adjustment's non-default priority round-trips intact through the parser.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentWithNonDefaultPriority_ShouldPreservePriority()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("urgent", adj => adj
                        .When(AdjustmentTrigger.Always)
                        .Action(AdjustmentAction.None)
                        .Priority(5)))));

        Assert.AreEqual(5, parsed[0].Adjustments[0].Priority);
    }

    /// <summary>
    /// Verifies that an explicit <c>NonWorking(false)</c> on the adjustment round-trips intact rather than reverting
    /// to <see langword="null" /> after parsing.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentNonWorkingFalse_ShouldPreserveExplicitFalse()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("working-override", adj => adj
                        .When(AdjustmentTrigger.IfWeekend)
                        .Action(AdjustmentAction.None)
                        .NonWorking(false)))));

        Assert.IsFalse(parsed[0].Adjustments[0].IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that the <see cref="ObservanceAdjustmentBuilder.HandlerKey" /> value round-trips intact.
    /// </summary>
    [TestMethod]
    public void RoundTrip_AdjustmentWithHandlerKey_ShouldPreserveHandlerKey()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("custom", adj => adj
                        .When(AdjustmentTrigger.Custom)
                        .Action(AdjustmentAction.Custom)
                        .HandlerKey("my-custom-handler")))));

        Assert.AreEqual("my-custom-handler", parsed[0].Adjustments[0].HandlerKey);
    }

    // ============================================================================
    // Hierarchical structures
    // ============================================================================

    /// <summary>
    /// Verifies that multiple rules under the same notable date name round-trip with each rule's identifying fields
    /// preserved and the canonical date name shared.
    /// </summary>
    [TestMethod]
    public void RoundTrip_MultipleRulesPerDate_ShouldPreserveEveryVariant()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("King's Birthday", date => date
                .AddRule(rule => rule
                    .RuleName("queensland")
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU-QLD")
                    .DayOfWeekInMonth(10, DayOfWeek.Monday, WeekOfMonthOrdinal.First))
                .AddRule(rule => rule
                    .RuleName("other-states")
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU-NSW,AU-VIC,AU-SA,AU-TAS,AU-ACT,AU-NT")
                    .DayOfWeekInMonth(6, DayOfWeek.Monday, WeekOfMonthOrdinal.Second))
                .AddRule(rule => rule
                    .RuleName("western-australia")
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU-WA")
                    .DayOfWeekInMonth(9, DayOfWeek.Monday, WeekOfMonthOrdinal.Last))));

        Assert.HasCount(3, parsed);
        Assert.IsTrue(parsed.All(r => r.Name == "King's Birthday"));
        Assert.AreEqual("queensland", parsed[0].RuleName);
        Assert.AreEqual("other-states", parsed[1].RuleName);
        Assert.AreEqual("western-australia", parsed[2].RuleName);
        Assert.AreEqual(10, parsed[0].Month);
        Assert.AreEqual(6, parsed[1].Month);
        Assert.AreEqual(9, parsed[2].Month);
    }

    /// <summary>
    /// Verifies that multiple notable dates in the same document each round-trip with their rules preserved and
    /// addition order maintained.
    /// </summary>
    [TestMethod]
    public void RoundTrip_MultipleDatesPerDocument_ShouldPreserveOrderAndRules()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 25)))
            .AddDate("Boxing Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 26)))
            .AddDate("New Year's Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(1, 1))));

        Assert.HasCount(3, parsed);
        Assert.AreEqual("Christmas Day", parsed[0].Name);
        Assert.AreEqual("Boxing Day", parsed[1].Name);
        Assert.AreEqual("New Year's Day", parsed[2].Name);
    }

    /// <summary>
    /// Verifies that multiple adjustments on the same rule round-trip with each retaining its unique key,
    /// trigger, action, and ordering.
    /// </summary>
    [TestMethod]
    public void RoundTrip_MultipleAdjustmentsPerRule_ShouldPreserveOrderAndKeys()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("saturday-shift", adj => adj
                        .When(AdjustmentTrigger.IfDayOfWeek)
                        .OnDayOfWeek(DayOfWeek.Saturday)
                        .Action(AdjustmentAction.MoveToPreviousWeekday))
                    .AddAdjustment("sunday-shift", adj => adj
                        .When(AdjustmentTrigger.IfDayOfWeek)
                        .OnDayOfWeek(DayOfWeek.Sunday)
                        .Action(AdjustmentAction.MoveToNextWeekday))
                    .AddAdjustment("custom-rule", adj => adj
                        .When(AdjustmentTrigger.Custom)
                        .Action(AdjustmentAction.Custom)
                        .HandlerKey("special-case")))));

        ImmutableArray<ObservanceAdjustment> adjustments = parsed[0].Adjustments;
        Assert.HasCount(3, adjustments);
        Assert.AreEqual("saturday-shift", adjustments[0].Key);
        Assert.AreEqual("sunday-shift", adjustments[1].Key);
        Assert.AreEqual("custom-rule", adjustments[2].Key);
        Assert.AreEqual(DayOfWeek.Saturday, adjustments[0].DayOfWeek);
        Assert.AreEqual(DayOfWeek.Sunday, adjustments[1].DayOfWeek);
        Assert.AreEqual("special-case", adjustments[2].HandlerKey);
    }

    /// <summary>
    /// Verifies that a fully-populated document with multiple dates, multi-variant rules, scoped adjustments, and
    /// every category of field round-trips with all values preserved.
    /// </summary>
    [TestMethod]
    public void RoundTrip_ComprehensiveNestedDocument_ShouldPreserveEntireGraph()
    {
        List<NotableDateRule> parsed = BuildAndReparse(b => b
            .AddDate("Australia Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU")
                    .NonWorking()
                    .FirstYear(1994)
                    .Priority(10)
                    .Comment("National public holiday")
                    .AddTag("Federal")
                    .Fixed(1, 26)
                    .AddAdjustment("weekend-roll", adj => adj
                        .When(AdjustmentTrigger.IfWeekend)
                        .Action(AdjustmentAction.MoveToNextWeekday)
                        .NonWorking())))
            .AddDate("Easter Sunday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Algorithm(key: "easter-gregorian")
                    .AddTag("Christian")))
            .AddDate("Easter Monday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .OffsetFromAnchor("Easter Sunday", 1)
                    .NonWorking()))
            .AddDate("Hanukkah", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .CalendarType(typeof(HebrewCalendar))
                    .Duration(8)
                    .Fixed("Kislev", 25))));

        Assert.HasCount(4, parsed);

        // Australia Day
        Assert.AreEqual("Australia Day", parsed[0].Name);
        Assert.AreEqual(1, parsed[0].Month);
        Assert.AreEqual(26, parsed[0].Day);
        Assert.AreEqual("AU", parsed[0].TerritoryCode);
        Assert.IsTrue(parsed[0].IsNonWorkingDay);
        Assert.HasCount(1, parsed[0].Adjustments);
        Assert.AreEqual("weekend-roll", parsed[0].Adjustments[0].Key);

        // Easter Sunday
        Assert.AreEqual("Easter Sunday", parsed[1].Name);
        Assert.AreEqual(DateResolutionStrategy.Algorithm, parsed[1].Strategy);
        Assert.AreEqual("easter-gregorian", parsed[1].AlgorithmKey);

        // Easter Monday
        Assert.AreEqual("Easter Monday", parsed[2].Name);
        Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, parsed[2].Strategy);
        Assert.AreEqual("Easter Sunday", parsed[2].AnchorRuleName);
        Assert.AreEqual(1, parsed[2].OffsetDays);

        // Hanukkah
        Assert.AreEqual("Hanukkah", parsed[3].Name);
        Assert.AreEqual(typeof(HebrewCalendar), parsed[3].CalendarType);
        Assert.AreEqual(8, parsed[3].DurationDays);
        Assert.AreEqual(3, parsed[3].Month);
        Assert.IsNull(parsed[3].CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that the <see cref="NotableDateDocumentBuilder.ToProvider" /> path yields an
    /// <see cref="INotableDateRuleProvider" /> whose rules match those produced by
    /// <see cref="NotableDateDocumentBuilder.Build" /> for the same comprehensive document.
    /// </summary>
    [TestMethod]
    public void RoundTrip_ToProvider_ShouldYieldRulesEquivalentToBuild()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 25)))
            .AddDate("Easter Sunday", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Religious).Algorithm(key: "easter-gregorian")));

        IReadOnlyList<NotableDateRule> built = builder.Build();
        var providerRules = builder.ToProvider().LoadRules().ToList();

        Assert.HasCount(built.Count, providerRules);
        for (var i = 0; i < built.Count; i++)
        {
            Assert.AreEqual(built[i].Name, providerRules[i].Name);
            Assert.AreEqual(built[i].Strategy, providerRules[i].Strategy);
            Assert.AreEqual(built[i].Month, providerRules[i].Month);
            Assert.AreEqual(built[i].Day, providerRules[i].Day);
            Assert.AreEqual(built[i].AlgorithmKey, providerRules[i].AlgorithmKey);
        }
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToXDocument" /> and
    /// <see cref="NotableDateDocumentBuilder.ToXml" /> produce equivalent payloads — the latter is the indented
    /// string serialisation of the former — for a comprehensive document.
    /// </summary>
    [TestMethod]
    public void RoundTrip_ToXDocumentAndToXml_ShouldProduceEquivalentPayloads()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Test A", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(1, 1)))
            .AddDate("Test B", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Religious).Algorithm(key: "k")));

        var doc = builder.ToXDocument();
        var fromXml = XDocument.Parse(builder.ToXml());

        // XNode.DeepEquals is not appropriate here: XDocument.Parse exposes the
        // default-namespace declaration as an explicit xmlns XAttribute on the root,
        // while the in-memory tree constructed by ToXDocument does not. Both render
        // to byte-identical XML via XmlWriter, so compare the serialised payloads.
        Assert.AreEqual(doc.ToString(), fromXml.ToString());
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    /// <summary>
    /// Builds, serialises, and re-parses the configured document, returning the parsed rules.
    /// </summary>
    /// <param name="configure">Callback that configures the builder under test.</param>
    /// <returns>The parsed rules.</returns>
    private static List<NotableDateRule> BuildAndReparse(Action<NotableDateDocumentBuilder> configure)
    {
        var builder = NotableDateDocumentBuilder.Create();
        configure(builder);
        return NotableDateRuleParser.ParseXml(builder.ToXml());
    }
}
