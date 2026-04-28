// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneration.Fibonacci.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

[TestClass]
public class FibonacciTests
{
    /// <summary>
    /// A known ordered list of Fibonacci numbers, starting from 0.
    /// </summary>
    public static readonly long[] Values =
    {
        0, 1, 1, 2, 3, 5, 8, 13, 21, 34,
        55, 89, 144, 233, 377, 610, 987,
        1597, 2584, 4181, 6765, 10946,
        17711, 28657, 46368, 75025, 121393,
        196418, 317811, 514229, 832040,
        1346269, 2178309, 3524578, 5702887
    };

    /// <summary>
    /// Verifies that <see cref="Fibonacci.Fibonacci" /> throws ArgumentOutOfRangeException when minimum is negative.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenMinimumIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SequenceGenerator.Fibonacci(-1, 10).ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fibonacci.Fibonacci" /> throws ArgumentOutOfRangeException when maximum is negative.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenMaximumIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SequenceGenerator.Fibonacci(0, -5).ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fibonacci.Fibonacci" /> throws ArgumentException when minimum is greater than maximum.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenMinimumGreaterThanMaximum_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            SequenceGenerator.Fibonacci(10, 5).ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fibonacci.Fibonacci" /> returns all expected values in the range 0 to 100.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenRangeIsZeroToHundred_ShouldReturnExpectedValues()
    {
        var expected = Values.Where(n => n >= 0 && n < 100).ToArray();
        var actual = SequenceGenerator.Fibonacci(0, 100).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Fibonacci.Fibonacci" /> returns only values between 5 and 15.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenRangeIsFiveToFifteen_ShouldReturnSubset()
    {
        var expected = Values.Where(n => n >= 5 && n < 15).ToArray();
        var actual = SequenceGenerator.Fibonacci(5, 15).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that values below the minimum threshold are not included in the actual.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenMinimumIsTwentyOne_ShouldExcludeLowerValues()
    {
        var expected = Values.Where(n => n >= 21 && n < 35).ToArray();
        var actual = SequenceGenerator.Fibonacci(21, 35).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Fibonacci.Fibonacci" /> returns an empty sequence when the range excludes all values.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenRangeIsOneToOne_ShouldReturnEmpty()
    {
        var actual = SequenceGenerator.Fibonacci(1, 1).ToArray();
        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Fibonacci.Fibonacci" /> returns a single matching value when the range bounds exactly one value.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenRangeBoundsSingleValue_ShouldReturnThatValue()
    {
        var expected = Values.Where(n => n >= 21 && n < 22).ToArray();
        var actual = SequenceGenerator.Fibonacci(21, 22).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that all Fibonacci numbers generated are non-negative when the upper limit is <see cref="long.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenMaxIsLongMaxValue_ShouldContainOnlyNonNegativeValues()
    {
        var values = SequenceGenerator.Fibonacci(0, long.MaxValue).ToList();
        Assert.IsTrue(values.All(v => v >= 0), "All values should be non-negative.");
    }

    /// <summary>
    /// Verifies that the Fibonacci sequence stops before exceeding <see cref="long.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void Fibonacci_WhenMaxIsLongMaxValue_ShouldStopBeforeExceedingLongMaxValue()
    {
        var values = SequenceGenerator.Fibonacci(0, long.MaxValue).ToList();
#if NETSTANDARD1_2_OR_GREATER
        Assert.IsTrue(values.Last() < long.MaxValue, "Last value should be less than long.MaxValue.");
#else
        Assert.IsTrue(values[^1] < long.MaxValue, "Last value should be less than long.MaxValue.");
#endif
    }
}
