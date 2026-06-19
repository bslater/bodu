// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.SnapToWorkingDayBackward.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDayBackward" /> returns a working-day input unchanged.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        DateOnly input = new(2026, 1, 6);

        Assert.AreEqual(input, input.SnapToWorkingDayBackward(Service, "XX"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.SnapToWorkingDayBackward" /> snaps a Saturday backward to the
    /// prior Friday.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenSaturday_ShouldReturnPriorFriday()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3).SnapToWorkingDayBackward(Service, "XX"));
    }
}
