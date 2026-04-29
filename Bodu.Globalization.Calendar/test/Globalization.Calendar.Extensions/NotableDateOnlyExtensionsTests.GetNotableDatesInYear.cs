// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.GetNotableDatesInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the year-scoped query returns every notable date for the input's year.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_WhenMultipleRulesExist_ShouldReturnAllDatesForYear()
    {
        NotableDateService service = BuildService(
            Fixed("New Year's Day", 1, 1),
            Fixed("Christmas Day", 12, 25));

        IReadOnlyList<NotableDate> result = new DateOnly(2026, 6, 15).GetNotableDatesInYear(service);

        Assert.AreEqual(2, result.Count);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDatesInYear_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 1).GetNotableDatesInYear(service: null!);
        });
    }
}
