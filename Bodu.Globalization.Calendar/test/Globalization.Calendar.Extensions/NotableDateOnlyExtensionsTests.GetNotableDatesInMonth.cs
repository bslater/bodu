// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.GetNotableDatesInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
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

        IReadOnlyList<NotableDate> result = new DateOnly(2026, 4, 15).GetNotableDatesInMonth(service);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Anzac Day", result[0].Name);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInMonth_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).GetNotableDatesInMonth(service: null!);
        });
    }
}
