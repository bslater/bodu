// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.WorkingDaysBetween.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that a one-week inclusive range counts five working days under the Saturday/Sunday weekend definition.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenOneWeekRange_ShouldReturnFiveWorkingDays()
    {
        NotableDateService service = BuildService();

        int result = new DateTime(2026, 1, 5).WorkingDaysBetween(new DateTime(2026, 1, 11), service);

        Assert.AreEqual(5, result);
    }

    /// <summary>
    /// Verifies that a non-working rule day inside the range is excluded from the count.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenNonWorkingRuleDayInRange_ShouldExcludeIt()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        int result = new DateTime(2026, 1, 5).WorkingDaysBetween(new DateTime(2026, 1, 11), service);

        Assert.AreEqual(4, result);
    }

    /// <summary>
    /// Verifies that swapped boundaries produce a non-negative count equal to the in-order range.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenBoundariesReversed_ShouldReturnNonNegativeSymmetricCount()
    {
        NotableDateService service = BuildService();

        int forward = new DateTime(2026, 1, 5).WorkingDaysBetween(new DateTime(2026, 1, 11), service);
        int reversed = new DateTime(2026, 1, 11).WorkingDaysBetween(new DateTime(2026, 1, 5), service);

        Assert.AreEqual(forward, reversed);
    }

    /// <summary>
    /// Verifies that a single-day range covering one working day returns one.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenSingleWorkingDay_ShouldReturnOne()
    {
        NotableDateService service = BuildService();

        int result = new DateTime(2026, 1, 6).WorkingDaysBetween(new DateTime(2026, 1, 6), service);

        Assert.AreEqual(1, result);
    }

    /// <summary>
    /// Verifies that a single-day range covering a non-working day returns zero.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenSingleNonWorkingDay_ShouldReturnZero()
    {
        NotableDateService service = BuildService();

        int result = new DateTime(2026, 1, 3).WorkingDaysBetween(new DateTime(2026, 1, 3), service);

        Assert.AreEqual(0, result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            int result = new DateTime(2026, 1, 5).WorkingDaysBetween(new DateTime(2026, 1, 11));

            Assert.AreEqual(5, result);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 5).WorkingDaysBetween(new DateTime(2026, 1, 11), service: null!);
        });
    }
}
