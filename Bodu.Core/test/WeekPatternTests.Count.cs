// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Count.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Count" /> returns seven when all days are selected.
    /// </summary>
    [TestMethod]
    public void Count_WhenAllDaysSelected_ShouldBeSeven() => Assert.AreEqual(7, WeekPattern.FromByte(127).Count);
    /// <summary>
    /// Verifies that <see cref="WeekPattern.Count" /> returns zero for an empty pattern.
    /// </summary>
    [TestMethod]
    public void Count_WhenEmpty_ShouldBeZero() => Assert.AreEqual(0, WeekPattern.Empty.Count);

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Count" /> accurately reflects the number of days selected
    /// when only a subset is set.
    /// </summary>
    [TestMethod]
    public void Count_WhenSomeDaysSelected_ShouldReflectSelection()
    {
        var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday);
        Assert.AreEqual(2, pattern.Count);
    }

}
