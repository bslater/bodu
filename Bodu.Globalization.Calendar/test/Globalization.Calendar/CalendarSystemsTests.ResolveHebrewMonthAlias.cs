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

    /// <summary>
    /// Verifies that a <see langword="null" /> Hebrew month alias throws <see cref="ArgumentNullException" /> rather
    /// than silently resolving to the <c>-1</c> "no such month" sentinel, so a null argument is distinguishable from
    /// an unknown-but-non-null alias.
    /// </summary>
    [TestMethod]
    public void ResolveHebrewMonthAlias_WhenAliasIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CalendarSystems.ResolveHebrewMonthAlias(null!, false);
        });
    }
}
