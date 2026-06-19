// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyBoundaryGuardTests.RelativeWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class StrategyBoundaryGuardTests
{
    /// <summary>
    /// Verifies that a relative-weekday rule whose seek rolls past year 9999 skips gracefully without throwing when a
    /// range query spans the boundary.
    /// </summary>
    [TestMethod]
    public void RelativeWeekdayInMonth_WhenSeekOverflowsAtYear9999_ShouldSkipWithoutThrowing()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.boundary-relative">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="r" displayName="R" category="Observance">
              <Rules><Rule id="x"><Strategy><RelativeWeekdayInMonth month="December" dayOfWeek="Friday" weekOrdinal="Last" relativeDayOfWeek="Saturday" direction="After" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        // The seek past the last Friday of December 9999 rolls into year 10000; resolution must not throw.
        IReadOnlyList<NotableDate> results = Build(xml)
            .Resolve(new DateRange(new DateOnly(9999, 1, 1), new DateOnly(9999, 12, 31)), Territory);

        Assert.AreEqual(0, results.Count(r => r.NotableDateId == "r"));
    }
}
