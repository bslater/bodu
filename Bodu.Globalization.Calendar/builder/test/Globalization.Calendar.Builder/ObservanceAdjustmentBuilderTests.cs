// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservanceAdjustmentBuilderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the validation behaviour of <see cref="ObservanceAdjustmentBuilder" />.
/// </summary>
[TestClass]
public class ObservanceAdjustmentBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ComparisonDate" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month is less than 1.
    /// </summary>
    [TestMethod]
    public void ComparisonDate_WhenMonthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.ComparisonDate(0, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ComparisonDate" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the month exceeds 12.
    /// </summary>
    [TestMethod]
    public void ComparisonDate_WhenMonthExceedsTwelve_ShouldThrowArgumentOutOfRangeException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.ComparisonDate(13, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ComparisonDate" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the day is less than 1.
    /// </summary>
    [TestMethod]
    public void ComparisonDate_WhenDayIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.ComparisonDate(1, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ComparisonDate" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the day exceeds 31.
    /// </summary>
    [TestMethod]
    public void ComparisonDate_WhenDayExceedsThirtyOne_ShouldThrowArgumentOutOfRangeException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.ComparisonDate(1, 32);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.Territory" /> throws <see cref="ArgumentNullException" />
    /// when the territory code is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Territory_WhenCodeIsNull_ShouldThrowArgumentNullException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.Territory(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.CalendarType" /> throws <see cref="ArgumentNullException" />
    /// when the calendar type is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CalendarType_WhenTypeIsNull_ShouldThrowArgumentNullException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.CalendarType(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.Target" /> throws an argument exception when the
    /// rule name is whitespace.
    /// </summary>
    [TestMethod]
    public void Target_WhenRuleNameIsWhitespace_ShouldThrowArgumentException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.Target("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.HandlerKey" /> throws an argument exception when the
    /// key is an empty string.
    /// </summary>
    [TestMethod]
    public void HandlerKey_WhenKeyIsEmpty_ShouldThrowArgumentException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.HandlerKey(string.Empty);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.AddHandlerParameter" /> throws
    /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddHandlerParameter_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddHandlerParameter(null!, "value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.AddHandlerParameter" /> throws an argument exception
    /// when the key is whitespace.
    /// </summary>
    [TestMethod]
    public void AddHandlerParameter_WhenKeyIsWhitespace_ShouldThrowArgumentException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddHandlerParameter("   ", "value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.AddHandlerParameter" /> throws
    /// <see cref="ArgumentNullException" /> when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddHandlerParameter_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddHandlerParameter("key", null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.MaxAdjustmentReachDays" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the day count is negative.
    /// </summary>
    [TestMethod]
    public void MaxAdjustmentReachDays_WhenNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ObservanceAdjustmentBuilder builder = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.MaxAdjustmentReachDays(-1);
        });
    }
}
