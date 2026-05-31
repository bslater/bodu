// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.EnumerateNonWorkingDays.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the enumeration yields the Saturday and Sunday inside a one-week range.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenOneWeekRange_ShouldYieldWeekend()
    {
        NotableDateService service = BuildService();

        DateTime[] result = new DateTime(2026, 1, 5).EnumerateNonWorkingDays(new DateTime(2026, 1, 11), service).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateTime(2026, 1, 10), new DateTime(2026, 1, 11) },
            result);
    }

    /// <summary>
    /// Verifies that a non-working rule day is included.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenNonWorkingRuleDayInRange_ShouldIncludeIt()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateTime[] result = new DateTime(2026, 1, 5).EnumerateNonWorkingDays(new DateTime(2026, 1, 11), service).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateTime(2026, 1, 7), new DateTime(2026, 1, 10), new DateTime(2026, 1, 11) },
            result);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 5).EnumerateNonWorkingDays(new DateTime(2026, 1, 11), service: null!);
        });
    }

    /// <summary>
    /// Verifies that swapping the start and end boundaries still yields an ascending sequence equal to the in-order range.
    /// </summary>
    [TestMethod]
    public void EnumerateNonWorkingDays_WhenBoundariesReversed_ShouldYieldAscendingSequence()
    {
        NotableDateService service = BuildService();

        DateTime[] forward = new DateTime(2026, 1, 5).EnumerateNonWorkingDays(new DateTime(2026, 1, 11), service).ToArray();
        DateTime[] reversed = new DateTime(2026, 1, 11).EnumerateNonWorkingDays(new DateTime(2026, 1, 5), service).ToArray();

        CollectionAssert.AreEqual(forward, reversed);
    }

    /// <summary>
    /// Verifies that a single-day range yields one non-working day for weekends and zero for weekdays.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(SingleDayNonWorkingYieldTestData))]
    public void EnumerateNonWorkingDays_WhenSingleDayRange_ShouldYieldExpectedCount(DateTime day, int expected)
    {
        NotableDateService service = BuildService();

        var actual = day.EnumerateNonWorkingDays(day, service).Count();

        Assert.AreEqual(expected, actual);
    }
}
