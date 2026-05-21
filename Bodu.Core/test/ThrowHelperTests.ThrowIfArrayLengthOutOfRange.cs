// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" />, when ArrayIsNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayLengthOutOfRange_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthOutOfRange(array!, 1, 10);
        });
    }
    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" /> contract with explicit
    /// ParamName assertions: null array → <see cref="ArgumentNullException" /> on the array parameter;
    /// length below min or above max → <see cref="ArgumentOutOfRangeException" /> on the array parameter.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array, or <c>-1</c> to pass <see langword="null" />.</param>
    /// <param name="minLength">Inclusive minimum.</param>
    /// <param name="maxLength">Inclusive maximum.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null array → ANE on array", -1, 1, 10, "ArgumentNullException", "array")]
    [DataRow("below min → AOORE on array", 0, 1, 10, "ArgumentOutOfRangeException", "array")]
    [DataRow("above max → AOORE on array", 11, 1, 10, "ArgumentOutOfRangeException", "array")]
    [DataRow("at min → pass", 1, 1, 10, "", "")]
    [DataRow("at max → pass", 10, 1, 10, "", "")]
    [DataRow("inside → pass", 5, 1, 10, "", "")]
    [DataRow("degenerate single value → pass", 7, 7, 7, "", "")]
    public void ThrowIfArrayLengthOutOfRange_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int arrayLength, int minLength, int maxLength, string expectedExceptionTypeName, string expectedParamName)
    {
        Array? array = arrayLength < 0 ? null : new int[arrayLength];
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthOutOfRange(array!, minLength, maxLength, "array"),
            expected,
            param);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthOutOfRange" />, when LengthIsOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10)]    // below min
    [DataRow(11, 1, 10)]   // above max
    [DataRow(5, 10, 20)]   // below min
    [DataRow(25, 10, 20)]  // above max
    public void ThrowIfArrayLengthOutOfRange_WhenLengthIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int arrayLength, int minLength, int maxLength)
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
