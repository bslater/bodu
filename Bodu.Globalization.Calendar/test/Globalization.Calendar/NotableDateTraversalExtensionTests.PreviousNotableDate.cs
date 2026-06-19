// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.PreviousNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that the previous notable date strictly before the input is the latest occurring before it.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenMultipleRulesExist_ShouldReturnLatestBeforeInput()
    {
        NotableDate? result = new DateOnly(2026, 6, 15).PreviousNotableDate(CalendarService, "XX");

        NotableDateAssert.AssertOccurrence(result, "anzac-day", new DateOnly(2026, 4, 25));
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

        NotableDateAssert.AssertOccurrence(result, "christmas-day", new DateOnly(2025, 12, 25));
    }

    /// <summary>
    /// Verifies that an input lying inside a multi-day span is treated as after the span's anchor; the previous search
    /// returns the same span. A 3 June 2026 input returns the festival anchored on 1 June.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenInputInsideMultiDaySpan_ShouldReturnSpan()
    {
        NotableDate? result = new DateOnly(2026, 6, 3).PreviousNotableDate(SpanService, "XX");

        NotableDateAssert.AssertOccurrence(result, "festival", new DateOnly(2026, 6, 1));
    }
}
