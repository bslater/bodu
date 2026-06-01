// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotPowerOfTwo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for accepted positive powers of two.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    [TestMethod]
    [DataRow("1 (2^0)", 1)]
    [DataRow("2 (2^1)", 2)]
    [DataRow("4 (2^2)", 4)]
    [DataRow("1024 (2^10)", 1024)]
    [DataRow("2^30", 1073741824)]
    public void ThrowIfNotPowerOfTwo_WhenValueIsAccepted_ShouldNotThrowAndReportNothing(string testName, int value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotPowerOfTwo(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for zero, negative inputs,
    /// and positive integers that are not powers of two.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    [TestMethod]
    [DataRow("zero", 0)]
    [DataRow("negative", -1)]
    [DataRow("MinValue", int.MinValue)]
    [DataRow("positive non-power 3", 3)]
    [DataRow("positive non-power 100", 100)]
    public void ThrowIfNotPowerOfTwo_WhenValueIsRejected_ShouldThrowOnValue(string testName, int value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotPowerOfTwo(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for a <see cref="long" /> value that is not a power of two,
    /// exercising the generic <c>IBinaryInteger&lt;T&gt;</c> constraint.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPowerOfTwo_WhenLongValueIsNotPowerOfTwo_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPowerOfTwo(4294967297L); // 2^32 + 1
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> works correctly with <see cref="long" />
    /// arguments, exercising the generic <c>IBinaryInteger&lt;T&gt;</c> constraint.
    /// </summary>
    [TestMethod]
    [DataRow(4294967296L)]  // 2^32 — valid power of two, exceeds int range
    [DataRow(1099511627776L)] // 2^40
    public void ThrowIfNotPowerOfTwo_WhenValueIsLongPowerOfTwo_ShouldNotThrow(long value) => ThrowHelper.ThrowIfNotPowerOfTwo(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is negative.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-2)]
    [DataRow(-4)]
    [DataRow(int.MinValue)]
    public void ThrowIfNotPowerOfTwo_WhenValueIsNegative_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPowerOfTwo(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is a positive integer that is not a power of two.
    /// </summary>
    [TestMethod]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(10)]
    [DataRow(100)]
    public void ThrowIfNotPowerOfTwo_WhenValueIsNotPowerOfTwo_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPowerOfTwo(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> does not throw when the value is a
    /// positive power of two.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(1024)]
    [DataRow(1073741824)] // 2^30
    public void ThrowIfNotPowerOfTwo_WhenValueIsPowerOfTwo_ShouldNotThrow(int value) => ThrowHelper.ThrowIfNotPowerOfTwo(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is zero.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPowerOfTwo_WhenValueIsZero_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPowerOfTwo(0);
        });
    }

}
