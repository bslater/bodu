// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.CoverageDayCollision.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies coverage-day collision arbitration for single-day queries (F-014): a multi-day span covering the queried
/// day and a singleton on that day are resolved together.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that for a single-day query, a multi-day span covering the day and a singleton on the day are
    /// arbitrated as one collision group, ordered by the documented precedence (here, ascending priority).
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenSingleDayCoveredBySpanAndSingleton_ShouldArbitrateTogether()
    {
        // A 5-day festival starting 1 June covers 3 June (its EndDate is 5 June); a singleton holiday lands on 3 June.
        NotableDateRule festival = Fixed("Festival", 6, 1, category: NotableDateCategory.Cultural) with { DurationDays = 5, Priority = 50 };
        NotableDateRule holiday = Fixed("Holiday", 6, 3, category: NotableDateCategory.Holiday) with { Priority = 10 };
        NotableDateService service = BuildService(festival, holiday);

        IReadOnlyList<NotableDate> onJune3 = service.GetNotableDates(new DateTime(2026, 6, 3));

        Assert.AreEqual(2, onJune3.Count, "Both the covering span and the singleton apply to 3 June.");
        Assert.AreEqual("Holiday", onJune3[0].Name, "Priority 10 outranks the span's priority 50 within the shared covered day.");
        Assert.AreEqual("Festival", onJune3[1].Name);
    }
}
