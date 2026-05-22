// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotPositiveMultipleOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> contract for the
    /// <see cref="int" /> instantiation with explicit ParamName assertions: zero, negative, or non-multiple
    /// values throw <see cref="ArgumentOutOfRangeException" /> on "value"; positive multiples pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    /// <param name="divisor">The required divisor.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("zero → throw on value", 0, 2, true)]
    [DataRow("negative → throw on value", -2, 2, true)]
    [DataRow("positive non-multiple → throw on value", 7, 2, true)]
    [DataRow("positive non-multiple of 3 → throw on value", 5, 3, true)]
    [DataRow("exact divisor → pass", 2, 2, false)]
    [DataRow("positive multiple → pass", 9, 3, false)]
    public void ThrowIfNotPositiveMultipleOf_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int value, int divisor, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotPositiveMultipleOf(value, divisor, nameof(value)),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for <see cref="long" /> values that are zero, negative, or not
    /// a multiple of the divisor, exercising the generic <c>IBinaryInteger&lt;T&gt;</c> constraint.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenLongValueIsNotMultiple_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPositiveMultipleOf(0L, 4L);
        });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPositiveMultipleOf(-8L, 4L);
        });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPositiveMultipleOf(7L, 4L);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> does not throw for <see cref="long" />
    /// values that are positive multiples of the divisor, exercising the generic <c>IBinaryInteger&lt;T&gt;</c>
    /// constraint.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenLongValueIsPositiveMultiple_ShouldNotThrow()
    {
        ThrowHelper.ThrowIfNotPositiveMultipleOf(4L, 4L);
        ThrowHelper.ThrowIfNotPositiveMultipleOf(4294967296L, 4L); // 2^32
        ThrowHelper.ThrowIfNotPositiveMultipleOf(1000000000000L, 1000L);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> does not throw when the
    /// integer value is positive and exactly divisible by the divisor.
    /// </summary>
    [TestMethod]
    [DataRow(2, 1)]
    [DataRow(4, 2)]
    [DataRow(9, 3)]
    [DataRow(10, 5)]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsPositiveAndMultiple_ShouldNotThrow(int value, int divisor) => ThrowHelper.ThrowIfNotPositiveMultipleOf(value, divisor);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the integer value is zero, negative, or not a multiple
    /// of the divisor.
    /// </summary>
    [TestMethod]
    [DataRow(0, 2)]   // zero is not positive
    [DataRow(-2, 2)]  // negative value
    [DataRow(7, 2)]   // not a multiple
    [DataRow(5, 3)]   // not a multiple
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsZeroNegativeOrNotMultiple_ShouldThrowExactly(int value, int divisor)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPositiveMultipleOf(value, divisor);
        });
    }

}
