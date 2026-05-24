// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.SnapToWorkingDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that a working day is returned unchanged.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var input = new DateOnly(2026, 1, 6);

        DateOnly result = input.SnapToWorkingDay(service);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that a Saturday snaps forward to the following Monday.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenInputIsSaturday_ShouldSnapToFollowingMonday()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 3).SnapToWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateOnly result = new DateOnly(2026, 1, 3).SnapToWorkingDay();

            Assert.AreEqual(new DateOnly(2026, 1, 5), result);
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
    public void SnapToWorkingDay_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 3).SnapToWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies that a non-working rule day snaps forward to the next working day.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenInputIsHoliday_ShouldSnapToNextWorkingDay()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 6, nonWorking: true));

        DateOnly result = new DateOnly(2026, 1, 6).SnapToWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 7), result);
    }
}
