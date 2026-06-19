// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystemsTests.ResolveHebrewMonthAlias.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class CalendarSystemsTests
{
    /// <summary>
    /// Verifies that an unrecognized Hebrew month alias resolves to a sentinel of <c>-1</c>.
    /// </summary>
    [TestMethod]
    public void ResolveHebrewMonthAlias_WhenUnknownAlias_ShouldReturnNegativeOne() =>
        Assert.AreEqual(-1, CalendarSystems.ResolveHebrewMonthAlias("NotAMonth", false));
}
