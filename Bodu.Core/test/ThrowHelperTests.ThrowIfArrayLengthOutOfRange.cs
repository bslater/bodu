// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthOutOfRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" />, when ArrayIsNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayLengthOutOfRange_WhenArrayIsNull_ShouldThrowExactly()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthOutOfRange(array!, 1, 10);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — when the array length is within the inclusive
    /// <c>[minLength, maxLength]</c> range.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array.</param>
    /// <param name="minLength">Inclusive minimum.</param>
    /// <param name="maxLength">Inclusive maximum.</param>
    [TestMethod]
    [DataRow("at min", 1, 1, 10)]
    [DataRow("at max", 10, 1, 10)]
    [DataRow("inside", 5, 1, 10)]
    [DataRow("degenerate single value", 7, 7, 7)]
    public void ThrowIfArrayLengthOutOfRange_WhenArrayLengthFits_ShouldNotThrowAndReportNothing(
        string testName, int arrayLength, int minLength, int maxLength)
    {
        Array array = new int[arrayLength];

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthOutOfRange(array, minLength, maxLength, "array"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" /> throws
    /// <see cref="ArgumentNullException" /> (when null) or <see cref="ArgumentOutOfRangeException" /> (when
    /// length falls outside <c>[minLength, maxLength]</c>), each with <c>ParamName == "array"</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array, or <c>-1</c> to pass <see langword="null" />.</param>
    /// <param name="minLength">Inclusive minimum.</param>
    /// <param name="maxLength">Inclusive maximum.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DataRow("null array", -1, 1, 10, "ArgumentNullException")]
    [DataRow("below min", 0, 1, 10, "ArgumentOutOfRangeException")]
    [DataRow("above max", 11, 1, 10, "ArgumentOutOfRangeException")]
    public void ThrowIfArrayLengthOutOfRange_WhenArrayIsRejected_ShouldThrowOnArray(
        string testName, int arrayLength, int minLength, int maxLength, string expectedExceptionTypeName)
    {
        Array? array = arrayLength < 0 ? null : new int[arrayLength];
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthOutOfRange(array!, minLength, maxLength, "array"),
            expected,
            "array");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" />, when LengthIsOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10)]    // below min
    [DataRow(11, 1, 10)]   // above max
    [DataRow(5, 10, 20)]   // below min
    [DataRow(25, 10, 20)]  // above max
    public void ThrowIfArrayLengthOutOfRange_WhenLengthIsOutOfRange_ShouldThrowExactly(int arrayLength, int minLength, int maxLength)
    {
        Array array = new int[arrayLength];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthOutOfRange(array, minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" />, when LengthIsWithinRange, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1, 10)]    // at min
    [DataRow(10, 1, 10)]   // at max
    [DataRow(5, 1, 10)]    // inside range
    [DataRow(0, 0, 0)]     // degenerate equal min and max
    [DataRow(7, 7, 7)]     // degenerate, only one valid length
    public void ThrowIfArrayLengthOutOfRange_WhenLengthIsWithinRange_ShouldNotThrow(int arrayLength, int minLength, int maxLength)
    {
        Array array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayLengthOutOfRange(array, minLength, maxLength);
    }

}
