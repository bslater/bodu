// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.EnumerateNonWorkingDays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the enumeration yields the Saturday and Sunday inside a one-week range.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenOneWeekRange_ShouldYieldWeekend()
    {
        NotableDateService service = BuildService();

        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), service).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11) },
            result);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenServiceIsNull_ShouldThrowArgumentNullExceptionEagerly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), service: null!);
        });
    }

    /// <summary>
    /// Verifies that swapping the start and end boundaries still yields an ascending sequence equal to the in-order range.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenBoundariesReversed_ShouldYieldAscendingSequence()
    {
        NotableDateService service = BuildService();

        DateOnly[] forward = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), service).ToArray();
        DateOnly[] reversed = new DateOnly(2026, 1, 11).EnumerateNonWorkingDays(new DateOnly(2026, 1, 5), service).ToArray();

        CollectionAssert.AreEqual(forward, reversed);
    }

    /// <summary>
    /// Verifies that a single-day range yields one non-working day for weekends and zero for weekdays.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(SingleDayNonWorkingYieldTestData), DynamicDataSourceType.Method)]
    public void EnumerateNonWorkingDays_WhenSingleDayRange_ShouldYieldExpectedCount(DateOnly day, int expected)
    {
        NotableDateService service = BuildService();

        int actual = day.EnumerateNonWorkingDays(day, service).Count();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a non-working rule day inside the range is included.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenNonWorkingRuleDayInRange_ShouldIncludeIt()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateOnly[] result = new DateOnly(2026, 1, 5).EnumerateNonWorkingDays(new DateOnly(2026, 1, 11), service).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 7), new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11) },
            result);
    }
}
