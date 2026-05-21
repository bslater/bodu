// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringLengthOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> contract with explicit
    /// ParamName assertions: null → <see cref="ArgumentNullException" /> on "value"; below-min or above-max
    /// → <see cref="ArgumentOutOfRangeException" /> on "value"; in-range → pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard, or <see langword="null" />.</param>
    /// <param name="minLength">Inclusive minimum length.</param>
    /// <param name="maxLength">Inclusive maximum length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null → ANE on value", null, 2, 10, "ArgumentNullException", "value")]
    [DataRow("below min → AOORE on value", "a", 2, 10, "ArgumentOutOfRangeException", "value")]
    [DataRow("empty below min → AOORE on value", "", 1, 5, "ArgumentOutOfRangeException", "value")]
    [DataRow("above max → AOORE on value", "abcdef", 1, 5, "ArgumentOutOfRangeException", "value")]
    [DataRow("at min → pass", "ab", 2, 10, "", "")]
    [DataRow("at max → pass", "abcdefghij", 2, 10, "", "")]
    [DataRow("inside → pass", "abcde", 2, 10, "", "")]
    [DataRow("min == 0 with empty → pass", "", 0, 5, "", "")]
    [DataRow("degenerate single length → pass", "x", 1, 1, "", "")]
    public void ThrowIfStringLengthOutOfRange_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, string? value, int minLength, int maxLength, string expectedExceptionTypeName, string expectedParamName)
    {
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfStringLengthOutOfRange(value!, minLength, maxLength, nameof(value)),
            expected,
            param);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the string length exceeds the maximum.
    /// </summary>
    [TestMethod]
    [DataRow("abcdef", 1, 5)]   // length 6 > max 5
    [DataRow("hello!", 2, 5)]   // length 6 > max 5
    [DataRow("abcde", 1, 4)]    // length 5 > max 4
    public void ThrowIfStringLengthOutOfRange_WhenLengthExceedsMaximum_ShouldThrowArgumentOutOfRangeException(string value, int minLength, int maxLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthOutOfRange(value, minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the string length is below the minimum.
    /// </summary>
    [TestMethod]
    [DataRow("a", 2, 10)]   // length 1 < min 2
    [DataRow("", 1, 5)]     // length 0 < min 1
    [DataRow("abc", 5, 10)] // length 3 < min 5
    public void ThrowIfStringLengthOutOfRange_WhenLengthIsBelowMinimum_ShouldThrowArgumentOutOfRangeException(string value, int minLength, int maxLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthOutOfRange(value, minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> does not throw when the string
    /// length is within the specified range (inclusive on both boundaries).
    /// </summary>
    [TestMethod]
    [DataRow("ab", 2, 10)]      // exactly at min
    [DataRow("abcdefghij", 2, 10)] // exactly at max
    [DataRow("abcde", 2, 10)]   // within range
    [DataRow("", 0, 5)]         // min = 0, empty string valid
    [DataRow("x", 1, 1)]        // min == max == length
    public void ThrowIfStringLengthOutOfRange_WhenLengthIsWithinRange_ShouldNotThrow(string value, int minLength, int maxLength) => ThrowHelper.ThrowIfStringLengthOutOfRange(value, minLength, maxLength);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentNullException" /> when the string value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStringLengthOutOfRange_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthOutOfRange(null!, 2, 10);
        });
    }

}
