// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystemsTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class CalendarSystemsTests
{
    /// <summary>
    /// Verifies that <see cref="CalendarSystems.Resolve" /> returns <see langword="null" /> for systems with no backing
    /// <see cref="System.Globalization.Calendar" /> (Gregorian and undefined values).
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSystemHasNoBackingCalendar_ShouldReturnNull()
    {
        Assert.IsNull(CalendarSystems.Resolve(CalendarSystem.Gregorian));
        Assert.IsNull(CalendarSystems.Resolve((CalendarSystem)999));
    }
}
