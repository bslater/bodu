// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.GetNotableDates.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that a single-day query returns notable dates whose anchor or span covers the input day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenDayMatchesAnchor_ShouldReturnMatchingNotableDate()
    {
        NotableDateService service = BuildService(Fixed("Christmas Day", 12, 25));

        IReadOnlyList<NotableDate> result = new DateOnly(2026, 12, 25).GetNotableDates(service);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Christmas Day", result[0].Name);
    }

    /// <summary>
    /// Verifies that a multi-day span is returned when querying any day inside the span.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenDayLiesInsideMultiDaySpan_ShouldReturnSpan()
    {
        NotableDateRule rule = Fixed("Festival", 6, 1) with { DurationDays = 5 };
        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> result = new DateOnly(2026, 6, 3).GetNotableDates(service);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Festival", result[0].Name);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).GetNotableDates(service: null!);
        });
    }
}
