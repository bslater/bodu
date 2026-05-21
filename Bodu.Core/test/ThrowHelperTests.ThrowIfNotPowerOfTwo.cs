// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotPowerOfTwo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> contract for the <see cref="int" />
    /// instantiation with explicit ParamName assertions: zero, negative, and positive non-power-of-two
    /// values throw <see cref="ArgumentOutOfRangeException" /> on "value"; <c>1, 2, 4, …, 2^30</c> pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("zero → throw on value", 0, true)]
    [DataRow("negative → throw on value", -1, true)]
    [DataRow("MinValue → throw on value", int.MinValue, true)]
    [DataRow("positive non-power → throw on value", 3, true)]
    [DataRow("positive non-power 100 → throw on value", 100, true)]
    [DataRow("1 (2^0) → pass", 1, false)]
    [DataRow("2 (2^1) → pass", 2, false)]
    [DataRow("4 (2^2) → pass", 4, false)]
    [DataRow("1024 (2^10) → pass", 1024, false)]
    [DataRow("2^30 → pass", 1073741824, false)]
    public void ThrowIfNotPowerOfTwo_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotPowerOfTwo(value, nameof(value)),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotPowerOfTwo{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for a <see cref="long" /> value that is not a power of two,
    /// exercising the generic <c>IBinaryInteger&lt;T&gt;</c> constraint.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPowerOfTwo_WhenLongValueIsNotPowerOfTwo_ShouldThrowArgumentOutOfRangeException()
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
    public void ThrowIfNotPowerOfTwo_WhenValueIsNegative_ShouldThrowArgumentOutOfRangeException(int value)
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
    public void ThrowIfNotPowerOfTwo_WhenValueIsNotPowerOfTwo_ShouldThrowArgumentOutOfRangeException(int value)
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
    public void ThrowIfNotPowerOfTwo_WhenValueIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotPowerOfTwo(0);
        });
    }

}
