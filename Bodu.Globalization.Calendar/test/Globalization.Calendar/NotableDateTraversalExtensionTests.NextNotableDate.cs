// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.NextNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that the next notable date strictly after the input is the earliest occurring after it.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenMultipleRulesExist_ShouldReturnEarliestAfterInput()
    {
        NotableDate? result = new DateOnly(2026, 6, 15).NextNotableDate(CalendarService, "XX");

        NotableDateAssert.AssertOccurrence(result, "christmas-day", new DateOnly(2026, 12, 25));
    }

    /// <summary>
    /// Verifies that when no rule fires later in the same year the next-notable-date search rolls into the next year.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenNoRuleLaterInYear_ShouldRollToNextYear()
    {
        NotableDate? result = new DateOnly(2026, 12, 31).NextNotableDate(CalendarService, "XX");

        NotableDateAssert.AssertOccurrence(result, "new-years-day", new DateOnly(2027, 1, 1));
    }

    /// <summary>
    /// Verifies that an input matching an occurrence is treated as not next; the following year's occurrence is
    /// returned instead.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenInputMatchesOccurrence_ShouldReturnFollowingOccurrence()
    {
        NotableDate? result = new DateOnly(2026, 1, 1).NextNotableDate(CalendarService, "XX");

        NotableDateAssert.AssertOccurrence(result, "festival", new DateOnly(2026, 4, 1));
    }

    /// <summary>
    /// Verifies that a filter restricts the next-notable-date search to matching occurrences only.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenFilterApplied_ShouldReturnFirstMatchingDate()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);

        NotableDate? result = new DateOnly(2026, 1, 15).NextNotableDate(CalendarService, "XX", filter);

        NotableDateAssert.AssertOccurrence(result, "anzac-day", new DateOnly(2026, 4, 25));
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

        NotableDateAssert.AssertOccurrence(result, "holiday", new DateOnly(2026, 8, 15));
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
