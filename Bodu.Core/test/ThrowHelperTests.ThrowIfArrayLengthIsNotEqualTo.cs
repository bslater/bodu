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
    /// Verifies the full <see cref="ThrowHelper.ThrowIfArrayLengthIsNotEqualTo" /> contract with explicit
    /// ParamName assertions: null array → <see cref="ArgumentNullException" /> on the array parameter;
    /// length mismatch → <see cref="ArgumentException" /> on the array parameter.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="arrayLength">Length of the array, or <c>-1</c> to pass <see langword="null" />.</param>
    /// <param name="expectedLength">The required exact length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null array → ANE on array", -1, 4, "ArgumentNullException", "array")]
    [DataRow("shorter than expected → AE on array", 3, 4, "ArgumentException", "array")]
    [DataRow("longer than expected → AE on array", 5, 4, "ArgumentException", "array")]
    [DataRow("exact match → pass", 4, 4, "", "")]
    [DataRow("both empty → pass", 0, 0, "", "")]
    public void ThrowIfArrayLengthIsNotEqualTo_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int arrayLength, int expectedLength, string expectedExceptionTypeName, string expectedParamName)
    {
        Array? array = arrayLength < 0 ? null : new int[arrayLength];
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthIsNotEqualTo(array, expectedLength, "array"),
            expected,
            param);
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
