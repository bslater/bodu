// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.GetNotableDatesInMonth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the month-scoped query returns notable dates whose anchor falls inside the input's calendar month.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_WhenMultipleRulesExist_ShouldReturnDatesForMonth()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Anzac Day", 4, 25),
            Fixed("Christmas Day", 12, 25));

        IReadOnlyList<NotableDate> result = new DateTime(2026, 4, 15).GetNotableDatesInMonth(service);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Anzac Day", result[0].Name);
    }

    /// <summary>
    /// Verifies that a multi-day span anchored in the previous month is returned when its days extend into the queried month.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_WhenSpanCrossesMonthBoundary_ShouldIncludeSpan()
    {
        NotableDateRule rule = Fixed("Festival", 1, 30) with { DurationDays = 5 };
        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> result = new DateTime(2026, 2, 15).GetNotableDatesInMonth(service);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Festival", result[0].Name);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).GetNotableDatesInMonth(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the explicit-service overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_WhenExplicitFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).GetNotableDatesInMonth(service, filter: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> filter to the ambient overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_WhenAmbientFilterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 1).GetNotableDatesInMonth(filter: null!);
        });
    }
}
