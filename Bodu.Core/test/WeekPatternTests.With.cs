// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.With.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{
    /// <summary>
    /// Verifies that <see cref="WeekPattern.With" /> returns a new instance that contains the added day
    /// alongside all days that were already selected.
    /// </summary>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    [DataRow(DayOfWeek.Friday)]
    [DataRow(DayOfWeek.Saturday)]
    public void With_WhenNewDayProvided_ShouldReturnInstanceWithDayAdded(DayOfWeek day)
    {
        WeekPattern result = WeekPattern.Empty.With(day);

        Assert.IsTrue(result.Contains(day));
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.With" /> does not affect days that were already selected —
    /// only the newly specified day is added.
    /// </summary>
    [TestMethod]
    public void With_WhenCalled_ShouldPreserveExistingDays()
    {
        var original = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday);
        WeekPattern result = original.With(DayOfWeek.Friday);

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that calling <see cref="WeekPattern.With" /> for a day that is already selected returns
    /// an instance equal to the original, without inflating <see cref="WeekPattern.Count" />.
    /// </summary>
    [TestMethod]
    public void With_WhenDayAlreadySelected_ShouldReturnEquivalentInstance()
    {
        var original = new WeekPattern(DayOfWeek.Monday);
        WeekPattern result = original.With(DayOfWeek.Monday);

        Assert.AreEqual(original, result);
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.With" /> does not mutate the original instance, confirming
    /// the immutable value-type contract.
    /// </summary>
    [TestMethod]
    public void With_WhenCalled_ShouldNotMutateOriginal()
    {
        var original = new WeekPattern(DayOfWeek.Monday);
        _ = original.With(DayOfWeek.Wednesday);

        Assert.IsFalse(original.Contains(DayOfWeek.Wednesday),
            "With must not modify the receiver; WeekPattern is immutable.");
        Assert.AreEqual(1, original.Count);
    }

    /// <summary>
    /// Verifies that chaining multiple <see cref="WeekPattern.With" /> calls correctly accumulates all
    /// specified days into the final instance.
    /// </summary>
    [TestMethod]
    public void With_WhenChained_ShouldAccumulateDays()
    {
        WeekPattern result = WeekPattern.Empty
            .With(DayOfWeek.Monday)
            .With(DayOfWeek.Wednesday)
            .With(DayOfWeek.Friday);

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.With" /> applied to all seven days produces an instance
    /// equal to a pattern constructed with all days explicitly.
    /// </summary>
    [TestMethod]
    public void With_WhenAllDaysAdded_ShouldProduceFullWeekPattern()
    {
        WeekPattern result = WeekPattern.Empty
            .With(DayOfWeek.Sunday)
            .With(DayOfWeek.Monday)
            .With(DayOfWeek.Tuesday)
            .With(DayOfWeek.Wednesday)
            .With(DayOfWeek.Thursday)
            .With(DayOfWeek.Friday)
            .With(DayOfWeek.Saturday);

        Assert.AreEqual(7, result.Count);
        Assert.AreEqual(WeekPattern.FromByte(0b1111111), result);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.With" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when an invalid <see cref="DayOfWeek" /> value is supplied.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(99)]
    public void With_WhenInvalidDay_ShouldThrowArgumentOutOfRangeException(int invalidDay)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = WeekPattern.Empty.With((DayOfWeek)invalidDay);
        });
    }
}
