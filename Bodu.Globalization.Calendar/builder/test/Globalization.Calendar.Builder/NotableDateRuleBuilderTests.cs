// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

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

    // ============================================================================
    // Strategy uniqueness — each builder commits to exactly one resolution strategy.
    // ============================================================================

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(int, int, bool, bool)" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to either Fixed overload.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the second Fixed call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void Fixed_WhenStrategyAlreadySet_ForNumericMonth_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Fixed(2, 2);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Fixed(string, int, bool, bool)" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to either Fixed overload.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the second Fixed call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void Fixed_WhenStrategyAlreadySet_ForMonthToken_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Fixed("March", 17);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.DayOfWeekInMonth" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to itself.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the DayOfWeekInMonth call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void DayOfWeekInMonth_WhenStrategyAlreadySet_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fourth);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.OffsetFromAnchor" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to itself.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the OffsetFromAnchor call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void OffsetFromAnchor_WhenStrategyAlreadySet_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.OffsetFromAnchor("Easter Sunday", 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Algorithm" /> throws
    /// <see cref="InvalidOperationException" /> when any other strategy has already been selected on the
    /// builder, including a previous call to itself.
    /// </summary>
    /// <param name="firstStrategy">The strategy name applied to the builder before the Algorithm call.</param>
    [TestMethod]
    [DataRow("Fixed")]
    [DataRow("DayOfWeekInMonth")]
    [DataRow("OffsetFromAnchor")]
    [DataRow("Algorithm")]
    public void Algorithm_WhenStrategyAlreadySet_ShouldThrowInvalidOperationException(string firstStrategy)
    {
        NotableDateRuleBuilder builder = NewBuilderWithStrategy(firstStrategy);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Algorithm(key: "easter-gregorian");
        });
    }

    /// <summary>
    /// Creates a new <see cref="NotableDateRuleBuilder" /> with the named strategy already selected.
    /// </summary>
    /// <param name="strategy">The strategy identifier — one of <c>Fixed</c>, <c>DayOfWeekInMonth</c>, <c>OffsetFromAnchor</c>, or <c>Algorithm</c>.</param>
    /// <returns>A builder whose strategy has been applied; subsequent strategy calls must throw.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="strategy" /> is not a recognised strategy identifier.</exception>
    private static NotableDateRuleBuilder NewBuilderWithStrategy(string strategy) =>
        strategy switch
        {
            "Fixed" => new NotableDateRuleBuilder().Fixed(1, 1),
            "DayOfWeekInMonth" => new NotableDateRuleBuilder().DayOfWeekInMonth(1, DayOfWeek.Monday, WeekOfMonthOrdinal.First),
            "OffsetFromAnchor" => new NotableDateRuleBuilder().OffsetFromAnchor("anchor", 1),
            "Algorithm" => new NotableDateRuleBuilder().Algorithm(key: "algo"),
            _ => throw new ArgumentException($"Unknown strategy identifier: {strategy}", nameof(strategy)),
        };
}
