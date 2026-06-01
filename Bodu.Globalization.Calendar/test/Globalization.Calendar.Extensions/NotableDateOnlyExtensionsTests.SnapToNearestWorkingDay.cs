// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.SnapToNearestWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    public void SnapToNearestWorkingDay_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var input = new DateOnly(2026, 1, 6);

        DateOnly result = input.SnapToNearestWorkingDay(service);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that a Saturday snaps backward to the prior Friday.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenSaturdayClosestBackward_ShouldSnapBackward()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 3).SnapToNearestWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 2), result);
    }

    /// <summary>
    /// Verifies that a Sunday snaps forward to the next Monday.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenSundayClosestForward_ShouldSnapForward()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 4).SnapToNearestWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that when forward and backward distances are equal the forward result is preferred.
    /// </summary>
    [TestMethod]
    public void SnapToNearestWorkingDay_WhenForwardAndBackwardEquidistant_ShouldPreferForward()
    {
        // Wednesday 2026-01-07 marked non-working. Tue 1-06 and Thu 1-08 are both working days, each one day away.
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateOnly result = new DateOnly(2026, 1, 7).SnapToNearestWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 8), result);
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

            DateOnly result = new DateOnly(2026, 1, 4).SnapToNearestWorkingDay();

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
    public void SnapToNearestWorkingDay_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 3).SnapToNearestWorkingDay(service: null!);
        });
    }
}
