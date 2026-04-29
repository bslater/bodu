// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.SnapToWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that a working day is returned unchanged.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        DateTime input = new DateTime(2026, 1, 6, 14, 30, 0, DateTimeKind.Utc);

        DateTime result = input.SnapToWorkingDay(service);

        Assert.AreEqual(input, result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// Verifies that a Saturday snaps forward to the following Monday.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenInputIsSaturday_ShouldSnapToFollowingMonday()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 3).SnapToWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that a non-working rule day snaps forward to the next working day.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenInputIsHoliday_ShouldSnapToNextWorkingDay()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 6, nonWorking: true));

        DateTime result = new DateTime(2026, 1, 6).SnapToWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 7), result);
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

            DateTime result = new DateTime(2026, 1, 3).SnapToWorkingDay();

            Assert.AreEqual(new DateTime(2026, 1, 5), result);
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
    public void SnapToWorkingDay_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 3).SnapToWorkingDay(service: null!);
        });
    }
}
