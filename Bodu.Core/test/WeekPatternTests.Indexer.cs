// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that the indexer getter returns <see langword="false" /> for days that were not selected.
    /// </summary>
    [TestMethod]
    public void IndexerGet_WhenDayIsNotSelected_ShouldReturnFalse()
    {
        var pattern = new WeekPattern(DayOfWeek.Monday);
        Assert.IsFalse(pattern[DayOfWeek.Tuesday]);
    }
    /// <summary>
    /// Verifies that the indexer getter returns <see langword="true" /> for every day that was selected
    /// at construction.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidDays))]
    public void IndexerGet_WhenDayIsSelected_ShouldReturnTrue(DayOfWeek day)
    {
        var pattern = new WeekPattern(day);
        Assert.IsTrue(pattern[day]);
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="ArgumentOutOfRangeException" /> when an
    /// invalid <see cref="DayOfWeek" /> value is used.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(10)]
    public void IndexerGet_WhenInvalidDayIndex_ShouldThrowExactly(int invalidDayIndex)
    {
        var pattern = new WeekPattern();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = pattern[(DayOfWeek)invalidDayIndex];
        });
    }

    private static IEnumerable<object[]> GetValidDays()
    {
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            yield return new object[] { day };
    }

}
