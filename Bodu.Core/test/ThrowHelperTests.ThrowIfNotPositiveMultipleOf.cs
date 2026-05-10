// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotPositiveMultipleOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
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
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsZeroNegativeOrNotMultiple_ShouldThrowArgumentOutOfRangeException(int value, int divisor)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPositiveMultipleOf(value, divisor);
        });
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
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsPositiveAndMultiple_ShouldNotThrow(int value, int divisor)
    {
        ThrowHelper.ThrowIfNotPositiveMultipleOf(value, divisor);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for <see cref="long" /> values that are zero, negative, or not
    /// a multiple of the divisor, exercising the generic <c>IBinaryInteger&lt;T&gt;</c> constraint.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenLongValueIsNotMultiple_ShouldThrowArgumentOutOfRangeException()
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
}
