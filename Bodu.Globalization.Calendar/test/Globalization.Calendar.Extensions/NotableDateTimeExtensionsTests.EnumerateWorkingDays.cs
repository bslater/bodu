// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.EnumerateWorkingDays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the enumeration yields all weekdays in the range under the Saturday/Sunday weekend definition.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenOneWeekRange_ShouldYieldFiveDays()
    {
        NotableDateService service = BuildService();

        DateTime[] result = new DateTime(2026, 1, 5).EnumerateWorkingDays(new DateTime(2026, 1, 11), service).ToArray();

        Assert.AreEqual(5, result.Length);
        Assert.AreEqual(new DateTime(2026, 1, 5), result[0]);
        Assert.AreEqual(new DateTime(2026, 1, 9), result[4]);
    }

    /// <summary>
    /// Verifies that a non-working rule day is skipped.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenNonWorkingRuleDayInRange_ShouldSkipIt()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateTime[] result = new DateTime(2026, 1, 5).EnumerateWorkingDays(new DateTime(2026, 1, 9), service).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateTime(2026, 1, 5), new DateTime(2026, 1, 6), new DateTime(2026, 1, 8), new DateTime(2026, 1, 9) },
            result);
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" /> eagerly.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenServiceIsNull_ShouldThrowArgumentNullExceptionEagerly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 5).EnumerateWorkingDays(new DateTime(2026, 1, 11), service: null!);
        });
    }

    /// <summary>
    /// Verifies that swapping the start and end boundaries still yields an ascending sequence equal to the in-order range.
    /// </summary>
    [TestMethod]
    public void EnumerateWorkingDays_WhenBoundariesReversed_ShouldYieldAscendingSequence()
    {
        NotableDateService service = BuildService();

        DateTime[] forward = new DateTime(2026, 1, 5).EnumerateWorkingDays(new DateTime(2026, 1, 11), service).ToArray();
        DateTime[] reversed = new DateTime(2026, 1, 11).EnumerateWorkingDays(new DateTime(2026, 1, 5), service).ToArray();

        CollectionAssert.AreEqual(forward, reversed);
    }
}
