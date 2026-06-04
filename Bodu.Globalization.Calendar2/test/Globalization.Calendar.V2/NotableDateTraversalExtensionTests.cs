// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Ports the v1 enumeration and notable-date traversal matrices to the v2 explicit-service surface: enumerating
/// working, non-working and notable dates over one-week and single-day ranges, the next/previous non-working day
/// single step, and the next/previous notable date (earliest-after / latest-before, year roll, multi-day span advance,
/// and filter restriction). It also asserts <see cref="DateTime" /> and <see cref="DateTimeOffset" /> kind, offset and
/// time-of-day preservation on traversal.
/// </summary>
/// <remarks>
/// <para>
/// Reversed-range behaviour follows v2 semantics, which differ from v1: the ascending enumerators iterate
/// <c>start..end</c> and yield nothing when the bounds are reversed, and the date-range notable-date query throws
/// <see cref="ArgumentOutOfRangeException" /> for a reversed range rather than yielding an ascending sequence.
/// </para>
/// </remarks>
[TestClass]
public sealed class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// A fixture with a single non-working public holiday on 1 January.
    /// </summary>
    private const string HolidayXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.traversal">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A fixture with several fixed observances used to exercise notable-date traversal and enumeration ordering.
    /// </summary>
    private const string CalendarXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.calendar">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="anzac-day" displayName="Anzac Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="April" day="25" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="festival" displayName="Festival" category="Cultural" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="April" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="christmas-day" displayName="Christmas Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="December" day="25" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A fixture whose only observance spans five days from 1 June, used to verify multi-day span traversal.
    /// </summary>
    private const string SpanXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.span">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="festival" displayName="Festival" category="Cultural" defaultNonWorkingDay="false">
          <Rules><Rule id="x" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="August" day="15" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a service over the single-holiday fixture.
    /// </summary>
    private static INotableDateService HolidayService =>
        new NotableDateService(NotableDateResourceLoader.Load(HolidayXml));

    /// <summary>
    /// Gets a service over the multi-observance calendar fixture.
    /// </summary>
    private static INotableDateService CalendarService =>
        new NotableDateService(NotableDateResourceLoader.Load(CalendarXml));

    /// <summary>
    /// Gets a service over the multi-day span fixture.
    /// </summary>
    private static INotableDateService SpanService =>
        new NotableDateService(NotableDateResourceLoader.Load(SpanXml));

    /// <summary>
    /// Provides single-day ranges in January 2026 paired with the number of working days each yields under the default
    /// working week (1 for a weekday, 0 for a weekend day).
    /// </summary>
    /// <returns>A sequence of <c>(int y, int m, int d, int expected)</c> rows.</returns>
    public static IEnumerable<object[]> SingleDayWorkingYieldRows()
    {
        yield return new object[] { 2026, 1, 5, 1 };   // Monday
        yield return new object[] { 2026, 1, 6, 1 };   // Tuesday
        yield return new object[] { 2026, 1, 9, 1 };   // Friday
        yield return new object[] { 2026, 1, 10, 0 };  // Saturday
        yield return new object[] { 2026, 1, 11, 0 };  // Sunday
    }

    /// <summary>
    /// Provides single-day ranges in January 2026 paired with the number of non-working days each yields under the
    /// default working week (0 for a weekday, 1 for a weekend day).
    /// </summary>
    /// <returns>A sequence of <c>(int y, int m, int d, int expected)</c> rows.</returns>
    public static IEnumerable<object[]> SingleDayNonWorkingYieldRows()
    {
        yield return new object[] { 2026, 1, 5, 0 };   // Monday
        yield return new object[] { 2026, 1, 9, 0 };   // Friday
        yield return new object[] { 2026, 1, 10, 1 };  // Saturday
        yield return new object[] { 2026, 1, 11, 1 };  // Sunday
    }

    /// <summary>
    /// Verifies that enumerating working days over the first business week of 2026 yields the five weekdays in ascending
    /// order.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenOneWeekRange_ShouldYieldFiveWeekdays()
    {
        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 11), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateOnly(2026, 1, 5),
                new DateOnly(2026, 1, 6),
                new DateOnly(2026, 1, 7),
                new DateOnly(2026, 1, 8),
                new DateOnly(2026, 1, 9),
            },
            result);
    }

    /// <summary>
    /// Verifies that a non-working holiday inside the range is skipped by the working-day enumeration. With the holiday
    /// on Thursday 1 January 2026, enumerating 31 December 2025 to 2 January 2026 yields only the two weekday working
    /// days.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenHolidayInRange_ShouldSkipIt()
    {
        DateOnly[] result = new DateOnly(2025, 12, 31).EnumerateWorkingDays(new DateOnly(2026, 1, 2), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 2) },
            result);
    }

    /// <summary>
    /// Verifies the working-day yield for a single-day range across weekdays and weekend days.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected number of working days yielded.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(SingleDayWorkingYieldRows), DynamicDataSourceType.Method)]
    public void EnumerateWorkingDays_WhenSingleDayRange_ShouldYieldExpectedCount(int year, int month, int day, int expected)
    {
        DateOnly d = new(year, month, day);

        Assert.AreEqual(expected, d.EnumerateWorkingDays(d, HolidayService, "XX").Count());
    }

    /// <summary>
    /// Verifies that reversed boundaries yield an empty working-day sequence under v2 ascending-only enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenBoundariesReversed_ShouldYieldNothing()
    {
        DateOnly[] reversed = new DateOnly(2026, 1, 11).EnumerateWorkingDays(new DateOnly(2026, 1, 5), HolidayService, "XX").ToArray();

        Assert.AreEqual(0, reversed.Length);
    }

    /// <summary>
    /// Verifies that enumerating non-working days over the first business week of 2026 yields the Saturday and Sunday.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenOneWeekRange_ShouldYieldWeekend()
    {
        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11) },
            result);
    }

    /// <summary>
    /// Verifies that a non-working holiday inside the range is included by the non-working-day enumeration. The holiday
    /// on Thursday 1 January 2026 is yielded alongside the surrounding weekend.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenHolidayInRange_ShouldIncludeIt()
    {
        DateOnly[] result = new DateOnly(2025, 12, 31).EnumerateNonWorkingDays(new DateOnly(2026, 1, 5), HolidayService, "XX").ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4) },
            result);
    }

    /// <summary>
    /// Verifies the non-working-day yield for a single-day range across weekdays and weekend days.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected number of non-working days yielded.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(SingleDayNonWorkingYieldRows), DynamicDataSourceType.Method)]
    public void EnumerateNonWorkingDays_WhenSingleDayRange_ShouldYieldExpectedCount(int year, int month, int day, int expected)
    {
        DateOnly d = new(year, month, day);

        Assert.AreEqual(expected, d.EnumerateNonWorkingDays(d, HolidayService, "XX").Count());
    }

    /// <summary>
    /// Verifies that the next non-working day from a working day before the holiday lands on the holiday. Wednesday
    /// 31 December 2025's next non-working day is the 1 January 2026 holiday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenBeforeHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 31).NextNonWorkingDay(HolidayService, "XX"));
    }

    /// <summary>
    /// Verifies that the next non-working day from a mid-week working day lands on the weekend. Friday 2 January 2026's
    /// next non-working day is Saturday 3 January.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenMidWeek_ShouldReturnWeekend()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 2).NextNonWorkingDay(HolidayService, "XX"));
    }

    /// <summary>
    /// Verifies that the previous non-working day from a working day after the holiday lands on the holiday. Friday
    /// 2 January 2026's previous non-working day is the 1 January holiday.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenAfterHoliday_ShouldReturnHoliday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2).PreviousNonWorkingDay(HolidayService, "XX"));
    }

    /// <summary>
    /// Verifies that the enumeration over a range yields every notable date intersecting the inclusive bounds, ordered
    /// by date.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenRangeContainsRules_ShouldYieldMatchingDatesInOrder()
    {
        NotableDate[] result = new DateOnly(2026, 4, 1).EnumerateNotableDates(new DateOnly(2026, 12, 31), CalendarService, "XX").ToArray();

        // April 1 (festival), April 25 (anzac), December 25 (christmas).
        CollectionAssert.AreEqual(
            new[] { "festival", "anzac-day", "christmas-day" },
            result.Select(r => r.NotableDateId).ToArray());
    }

    /// <summary>
    /// Verifies that a category filter restricts the range enumeration to matching notable dates only.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenFilterApplied_ShouldYieldOnlyMatchingDates()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);

        NotableDate[] result = new DateOnly(2026, 1, 1).EnumerateNotableDates(new DateOnly(2026, 12, 31), CalendarService, "XX", filter).ToArray();

        CollectionAssert.AreEqual(new[] { "festival" }, result.Select(r => r.NotableDateId).ToArray());
    }

    /// <summary>
    /// Verifies that a reversed range throws <see cref="ArgumentOutOfRangeException" /> under the v2 strict-range policy.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenBoundariesReversed_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2026, 12, 31).EnumerateNotableDates(new DateOnly(2026, 1, 1), CalendarService, "XX");
        });
    }

    /// <summary>
    /// Verifies that the next notable date strictly after the input is the earliest occurring after it.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenMultipleRulesExist_ShouldReturnEarliestAfterInput()
    {
        NotableDate? result = new DateOnly(2026, 6, 15).NextNotableDate(CalendarService, "XX");

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateOnly(2026, 12, 25), result.Date);
        Assert.AreEqual("christmas-day", result.NotableDateId);
    }

    /// <summary>
    /// Verifies that when no rule fires later in the same year the next-notable-date search rolls into the next year.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenNoRuleLaterInYear_ShouldRollToNextYear()
    {
        NotableDate? result = new DateOnly(2026, 12, 31).NextNotableDate(CalendarService, "XX");

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateOnly(2027, 1, 1), result.Date);
        Assert.AreEqual("new-years-day", result.NotableDateId);
    }

    /// <summary>
    /// Verifies that an input matching an occurrence is treated as not next; the following year's occurrence is
    /// returned instead.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenInputMatchesOccurrence_ShouldReturnFollowingOccurrence()
    {
        NotableDate? result = new DateOnly(2026, 1, 1).NextNotableDate(CalendarService, "XX");

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateOnly(2026, 4, 1), result.Date);
        Assert.AreEqual("festival", result.NotableDateId);
    }

    /// <summary>
    /// Verifies that a filter restricts the next-notable-date search to matching occurrences only.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenFilterApplied_ShouldReturnFirstMatchingDate()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);

        NotableDate? result = new DateOnly(2026, 1, 15).NextNotableDate(CalendarService, "XX", filter);

        Assert.IsNotNull(result);
        Assert.AreEqual("anzac-day", result.NotableDateId);
    }

    /// <summary>
    /// Verifies that an input lying inside a multi-day span is treated as past the span's anchor; the search advances to
    /// the next distinct occurrence. The festival span begins 1 June 2026, so a 3 June input advances to the 15 August
    /// holiday.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenInputInsideMultiDaySpan_ShouldAdvancePastSpan()
    {
        NotableDate? result = new DateOnly(2026, 6, 3).NextNotableDate(SpanService, "XX");

        Assert.IsNotNull(result);
        Assert.AreEqual("holiday", result.NotableDateId);
        Assert.AreEqual(new DateOnly(2026, 8, 15), result.Date);
    }

    /// <summary>
    /// Verifies that, with no occurrences, the next-notable-date search returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenNoOccurrences_ShouldReturnNull()
    {
        Assert.IsNull(new DateOnly(2026, 1, 1).NextNotableDate(HolidayService, "ZZ", NotableDateFilter.WithId("never")));
    }

    /// <summary>
    /// Verifies that the previous notable date strictly before the input is the latest occurring before it.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenMultipleRulesExist_ShouldReturnLatestBeforeInput()
    {
        NotableDate? result = new DateOnly(2026, 6, 15).PreviousNotableDate(CalendarService, "XX");

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateOnly(2026, 4, 25), result.Date);
        Assert.AreEqual("anzac-day", result.NotableDateId);
    }

    /// <summary>
    /// Verifies that when no rule fires earlier in the same year the previous-notable-date search rolls into the prior
    /// year.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenNoRuleEarlierInYear_ShouldRollToPreviousYear()
    {
        // Before 1 January 2026 the latest prior occurrence is Christmas Day 2025.
        NotableDate? result = new DateOnly(2026, 1, 1).PreviousNotableDate(CalendarService, "XX");

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateOnly(2025, 12, 25), result.Date);
        Assert.AreEqual("christmas-day", result.NotableDateId);
    }

    /// <summary>
    /// Verifies that an input lying inside a multi-day span is treated as after the span's anchor; the previous search
    /// returns the same span. A 3 June 2026 input returns the festival anchored on 1 June.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenInputInsideMultiDaySpan_ShouldReturnSpan()
    {
        NotableDate? result = new DateOnly(2026, 6, 3).PreviousNotableDate(SpanService, "XX");

        Assert.IsNotNull(result);
        Assert.AreEqual("festival", result.NotableDateId);
        Assert.AreEqual(new DateOnly(2026, 6, 1), result.Date);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTime" /> next working day skips the holiday and weekend while preserving the
    /// time-of-day and kind. Wednesday 31 December 2025 advances to Friday 2 January 2026 (the 1 January holiday is
    /// skipped).
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_OnDateTime_ShouldPreserveTimeAndKind()
    {
        DateTime start = new(2025, 12, 31, 14, 30, 0, DateTimeKind.Utc);

        DateTime next = start.NextWorkingDay(HolidayService, "XX");

        Assert.AreEqual(new DateOnly(2026, 1, 2), DateOnly.FromDateTime(next));
        Assert.AreEqual(new TimeSpan(14, 30, 0), next.TimeOfDay);
        Assert.AreEqual(DateTimeKind.Utc, next.Kind);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTime" /> previous non-working day lands on the holiday while preserving the
    /// time-of-day and kind.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_OnDateTime_ShouldPreserveTimeAndKind()
    {
        DateTime result = new DateTime(2026, 1, 2, 8, 15, 0, DateTimeKind.Local).PreviousNonWorkingDay(HolidayService, "XX");

        Assert.AreEqual(new DateTime(2026, 1, 1, 8, 15, 0, DateTimeKind.Local), result);
        Assert.AreEqual(DateTimeKind.Local, result.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime" /> signed working-day arithmetic preserves the time-of-day and kind across the
    /// holiday and weekend.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_OnDateTime_ShouldPreserveTimeAndKind()
    {
        DateTime start = new(2025, 12, 31, 6, 0, 0, DateTimeKind.Unspecified);

        DateTime result = start.AddWorkingDays(3, HolidayService, "XX");

        Assert.AreEqual(new DateOnly(2026, 1, 6), DateOnly.FromDateTime(result));
        Assert.AreEqual(new TimeSpan(6, 0, 0), result.TimeOfDay);
        Assert.AreEqual(DateTimeKind.Unspecified, result.Kind);
    }

    /// <summary>
    /// Verifies that enumerating <see cref="DateTime" /> non-working days reattaches the start's time-of-day and kind to
    /// each yielded value.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_OnDateTime_ShouldReattachStartTimeAndKind()
    {
        List<DateTime> days = new DateTime(2026, 1, 1, 6, 15, 0, DateTimeKind.Utc)
            .EnumerateNonWorkingDays(new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc), HolidayService, "XX")
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 1, 6, 15, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 3, 6, 15, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 4, 6, 15, 0, DateTimeKind.Utc),
            },
            days);
        Assert.IsTrue(days.TrueForAll(d => d.Kind == DateTimeKind.Utc));
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> next working day skips the holiday while preserving the
    /// time-of-day and offset.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_OnDateTimeOffset_ShouldPreserveTimeAndOffset()
    {
        DateTimeOffset start = new(2025, 12, 31, 14, 30, 0, TimeSpan.FromHours(5));

        DateTimeOffset next = start.NextWorkingDay(HolidayService, "XX");

        Assert.AreEqual(new DateOnly(2026, 1, 2), DateOnly.FromDateTime(next.DateTime));
        Assert.AreEqual(new TimeSpan(14, 30, 0), next.TimeOfDay);
        Assert.AreEqual(TimeSpan.FromHours(5), next.Offset);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeOffset" /> signed working-day arithmetic preserves the time-of-day and offset.
    /// From a Friday it advances one working day onto the following Monday in the same offset.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_OnDateTimeOffset_ShouldPreserveTimeAndOffset()
    {
        // 2026-05-15 is a Friday.
        DateTimeOffset friday = new(2026, 5, 15, 9, 30, 0, TimeSpan.FromHours(-8));

        DateTimeOffset monday = friday.AddWorkingDays(1, HolidayService, "XX");

        Assert.AreEqual(new DateOnly(2026, 5, 18), DateOnly.FromDateTime(monday.DateTime));
        Assert.AreEqual(new TimeSpan(9, 30, 0), monday.TimeOfDay);
        Assert.AreEqual(TimeSpan.FromHours(-8), monday.Offset);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly from the
    /// working-day enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 11), null!, "XX");
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" /> from the
    /// next-notable-date search.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).NextNotableDate(null!, "XX");
        });
    }
}
