// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.EnumerateWorkingDays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the enumeration yields all weekdays in the range under the Saturday/Sunday weekend definition.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenOneWeekRange_ShouldYieldFiveDays()
    {
        NotableDateService service = BuildService();

        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 11), service).ToArray();

        Assert.AreEqual(5, result.Length);
        Assert.AreEqual(new DateOnly(2026, 1, 5), result[0]);
        Assert.AreEqual(new DateOnly(2026, 1, 9), result[4]);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenServiceIsNull_ShouldThrowArgumentNullExceptionEagerly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 11), service: null!);
        });
    }

    /// <summary>
    /// Verifies that swapping the start and end boundaries still yields an ascending sequence equal to the in-order range.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenBoundariesReversed_ShouldYieldAscendingSequence()
    {
        NotableDateService service = BuildService();

        DateOnly[] forward = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 11), service).ToArray();
        DateOnly[] reversed = new DateOnly(2026, 1, 11).EnumerateWorkingDays(new DateOnly(2026, 1, 5), service).ToArray();

        CollectionAssert.AreEqual(forward, reversed);
    }

    /// <summary>
    /// Verifies that a single-day range yields one working day for weekdays and zero for weekends.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(SingleDayWorkingYieldTestData))]
    public void EnumerateWorkingDays_WhenSingleDayRange_ShouldYieldExpectedCount(DateOnly day, int expected)
    {
        NotableDateService service = BuildService();

        int actual = day.EnumerateWorkingDays(day, service).Count();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a non-working rule day inside the range is skipped.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenNonWorkingRuleDayInRange_ShouldSkipIt()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateWorkingDays(new DateOnly(2026, 1, 9), service).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 8), new DateOnly(2026, 1, 9) },
            result);
    }
}
