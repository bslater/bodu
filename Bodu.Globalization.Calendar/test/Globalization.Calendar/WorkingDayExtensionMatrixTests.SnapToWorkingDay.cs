// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.SnapToWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDay" /> returns a working-day input unchanged.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        DateOnly input = new(2026, 1, 6);

        Assert.AreEqual(input, input.SnapToWorkingDay(Service, "XX"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDay" /> snaps a Saturday forward to the following
    /// Monday.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDay_WhenSaturday_ShouldReturnFollowingMonday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 3).SnapToWorkingDay(Service, "XX"));
    }
}
