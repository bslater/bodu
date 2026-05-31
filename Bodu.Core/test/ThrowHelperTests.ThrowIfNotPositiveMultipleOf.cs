// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotPositiveMultipleOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for accepted positive multiples of the divisor.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    /// <param name="divisor">The required divisor.</param>
    [TestMethod]
    [DataRow("exact divisor", 2, 2)]
    [DataRow("positive multiple", 9, 3)]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsAccepted_ShouldNotThrowAndReportNothing(string testName, int value, int divisor) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotPositiveMultipleOf(value, divisor, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPositiveMultipleOf{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for zero, negative
    /// inputs, and positive integers that are not multiples of the divisor.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    /// <param name="divisor">The required divisor.</param>
    [TestMethod]
    [DataRow("zero", 0, 2)]
    [DataRow("negative", -2, 2)]
    [DataRow("positive non-multiple", 7, 2)]
    [DataRow("positive non-multiple of 3", 5, 3)]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsRejected_ShouldThrowOnValue(string testName, int value, int divisor) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotPositiveMultipleOf(value, divisor, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

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
