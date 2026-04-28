// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the validation behaviour of <see cref="NotableDateRuleBuilder" />.
/// </summary>
[TestClass]
public class NotableDateRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month is less than 1.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenMonthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Fixed(0, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month exceeds 13.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenMonthExceedsThirteen_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Fixed(14, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the day is less than 1.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenDayIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Fixed(1, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.DayOfWeekInMonth" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month is less than 1.
    /// </summary>
    [TestMethod]
    public void DayOfWeekInMonth_WhenMonthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.DayOfWeekInMonth(0, DayOfWeek.Monday, WeekOfMonthOrdinal.First);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.DayOfWeekInMonth" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month exceeds 12.
    /// </summary>
    [TestMethod]
    public void DayOfWeekInMonth_WhenMonthExceedsTwelve_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.DayOfWeekInMonth(13, DayOfWeek.Monday, WeekOfMonthOrdinal.First);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.OffsetFromAnchor" /> throws <see cref="ArgumentNullException" />
    /// when the anchor rule name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void OffsetFromAnchor_WhenAnchorRuleNameIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.OffsetFromAnchor(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Territory" /> throws an argument exception
    /// when the territory code is whitespace.
    /// </summary>
    [TestMethod]
    public void Territory_WhenCodeIsWhitespace_ShouldThrowArgumentException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.Territory("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Duration" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the value is less than 1.
    /// </summary>
    [TestMethod]
    public void Duration_WhenLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.Duration(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.OccurrenceYears" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the value is less than 1.
    /// </summary>
    [TestMethod]
    public void OccurrenceYears_WhenLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.OccurrenceYears(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.AddAdjustment" /> throws <see cref="ArgumentNullException" />
    /// when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddAdjustment_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddAdjustment(null!, _ => { });
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.AddAdjustment" /> throws <see cref="ArgumentNullException" />
    /// when the configure callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddAdjustment_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateRuleBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddAdjustment("key", null!);
        });
    }
}
