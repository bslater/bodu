// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.SnapToNearestWorkingDay.cs" company="PlaceholderCompany">
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
    public void SnapToNearestWorkingDay_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        DateTime input = new DateTime(2026, 1, 6);

        DateTime result = input.SnapToNearestWorkingDay(service);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that a Saturday is closer to Friday (1 day backward) than to Monday (2 days forward) and snaps backward.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenSaturdayClosestBackward_ShouldSnapBackward()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 3).SnapToNearestWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 2), result);
    }

    /// <summary>
    /// Verifies that a Sunday is closer to Monday (1 day forward) than to Friday (2 days backward) and snaps forward.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenSundayClosestForward_ShouldSnapForward()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 4).SnapToNearestWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that when forward and backward distances are equal the forward result is preferred.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenForwardAndBackwardEquidistant_ShouldPreferForward()
    {
        // Wednesday 2026-01-07 marked non-working. Tue 1-06 and Thu 1-08 are both working days, each one day away.
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateTime result = new DateTime(2026, 1, 7).SnapToNearestWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateTime result = new DateTime(2026, 1, 4).SnapToNearestWorkingDay();

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
    public void SnapToNearestWorkingDay_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 3).SnapToNearestWorkingDay(service: null!);
        });
    }
}
