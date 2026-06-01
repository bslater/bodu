// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringLengthOutOfRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — when the string length is within the inclusive
    /// <c>[minLength, maxLength]</c> range.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="minLength">Inclusive minimum length.</param>
    /// <param name="maxLength">Inclusive maximum length.</param>
    [TestMethod]
    [DataRow("at min", "ab", 2, 10)]
    [DataRow("at max", "abcdefghij", 2, 10)]
    [DataRow("inside", "abcde", 2, 10)]
    [DataRow("min == 0 with empty", "", 0, 5)]
    [DataRow("degenerate single length", "x", 1, 1)]
    public void ThrowIfStringLengthOutOfRange_WhenValueFits_ShouldNotThrowAndReportNothing(
        string testName, string value, int minLength, int maxLength) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfStringLengthOutOfRange(value, minLength, maxLength, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentNullException" /> (when null) or <see cref="ArgumentOutOfRangeException" /> (when
    /// the length falls outside <c>[minLength, maxLength]</c>), each with <c>ParamName == "value"</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="minLength">Inclusive minimum length.</param>
    /// <param name="maxLength">Inclusive maximum length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DataRow("null", null, 2, 10, "ArgumentNullException")]
    [DataRow("below min", "a", 2, 10, "ArgumentOutOfRangeException")]
    [DataRow("empty below min", "", 1, 5, "ArgumentOutOfRangeException")]
    [DataRow("above max", "abcdef", 1, 5, "ArgumentOutOfRangeException")]
    public void ThrowIfStringLengthOutOfRange_WhenValueIsRejected_ShouldThrowOnValue(
        string testName, string? value, int minLength, int maxLength, string expectedExceptionTypeName)
    {
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfStringLengthOutOfRange(value!, minLength, maxLength, nameof(value)),
            expected,
            "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the string length exceeds the maximum.
    /// </summary>
    [TestMethod]
    [DataRow("abcdef", 1, 5)]   // length 6 > max 5
    [DataRow("hello!", 2, 5)]   // length 6 > max 5
    [DataRow("abcde", 1, 4)]    // length 5 > max 4
    public void ThrowIfStringLengthOutOfRange_WhenLengthExceedsMaximum_ShouldThrowExactly(string value, int minLength, int maxLength)
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
    public void ThrowIfStringLengthOutOfRange_WhenLengthIsBelowMinimum_ShouldThrowExactly(string value, int minLength, int maxLength)
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
    public void ThrowIfStringLengthOutOfRange_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthOutOfRange(null!, 2, 10);
        });
    }

}
