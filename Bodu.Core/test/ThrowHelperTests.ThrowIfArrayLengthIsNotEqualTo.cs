// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthIsNotEqualTo.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsNotEqualTo" />, when ArrayIsNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayLengthIsNotEqualTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, 4);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsNotEqualTo" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — when the array length matches the expected length
    /// exactly.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array.</param>
    /// <param name="expectedLength">The required exact length.</param>
    [TestMethod]
    [DataRow("exact match", 4, 4)]
    [DataRow("both empty", 0, 0)]
    public void ThrowIfArrayLengthIsNotEqualTo_WhenArrayLengthMatches_ShouldNotThrowAndReportNothing(
        string testName, int arrayLength, int expectedLength)
    {
        Array array = new int[arrayLength];

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, expectedLength, "array"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsNotEqualTo" /> throws
    /// <see cref="ArgumentNullException" /> (when null) or <see cref="ArgumentException" /> (when length
    /// differs), each with <c>ParamName == "array"</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array, or <c>-1</c> to pass <see langword="null" />.</param>
    /// <param name="expectedLength">The required exact length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DataRow("null array", -1, 4, "ArgumentNullException")]
    [DataRow("shorter than expected", 3, 4, "ArgumentException")]
    [DataRow("longer than expected", 5, 4, "ArgumentException")]
    public void ThrowIfArrayLengthIsNotEqualTo_WhenArrayIsRejected_ShouldThrowOnArray(
        string testName, int arrayLength, int expectedLength, string expectedExceptionTypeName)
    {
        Array? array = arrayLength < 0 ? null : new int[arrayLength];
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, expectedLength, "array"),
            expected,
            "array");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsNotEqualTo" />, when LengthDiffers, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 4)]
    [DataRow(3, 4)]
    [DataRow(5, 4)]
    [DataRow(10, 1)]
    public void ThrowIfArrayLengthIsNotEqualTo_WhenLengthDiffers_ShouldThrowExactly(int arrayLength, int expectedLength)
    {
        Array array = new int[arrayLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, expectedLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsNotEqualTo" />, when LengthMatches, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(4, 4)]
    [DataRow(16, 16)]
    public void ThrowIfArrayLengthIsNotEqualTo_WhenLengthMatches_ShouldNotThrow(int arrayLength, int expectedLength)
    {
        Array array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, expectedLength);
    }

}
