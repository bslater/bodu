// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.AddWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that adding working days forward skips the holiday and weekends. From Tuesday 31 December 2024, three
    /// working days forward are 2 January (Thursday), 3 January (Friday) and 6 January (Monday).
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenAddingForward_ShouldSkipNonWorkingDays()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 6), new DateOnly(2024, 12, 31).AddWorkingDays(3, Service, "XX"));
    }

    /// <summary>
    /// Verifies that subtracting working days skips the holiday and weekends. From Monday 6 January 2025, three working
    /// days back are 3 January (Friday), 2 January (Thursday) and 31 December 2024 (Tuesday).
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenSubtractingBackward_ShouldSkipNonWorkingDays()
    {
        Assert.AreEqual(new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 6).AddWorkingDays(-3, Service, "XX"));
    }
}
