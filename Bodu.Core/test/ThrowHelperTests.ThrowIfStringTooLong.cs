// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringTooLong.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — when the string length is at or below the inclusive
    /// maximum.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="maxLength">Inclusive maximum length.</param>
    [TestMethod]
    [DataRow("exactly at max", "abcde", 5)]
    [DataRow("empty vs max=0", "", 0)]
    [DataRow("below max", "abc", 10)]
    public void ThrowIfStringTooLong_WhenValueFits_ShouldNotThrowAndReportNothing(string testName, string value, int maxLength) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfStringTooLong(value, maxLength, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> throws <see cref="ArgumentNullException" />
    /// (when null) or <see cref="ArgumentOutOfRangeException" /> (when length exceeds the maximum), each with
    /// <c>ParamName == "value"</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="maxLength">Inclusive maximum length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DataRow("null", null, 10, "ArgumentNullException")]
    [DataRow("longer than max", "abcdef", 5, "ArgumentOutOfRangeException")]
    [DataRow("non-empty vs max=0", "x", 0, "ArgumentOutOfRangeException")]
    public void ThrowIfStringTooLong_WhenValueIsRejected_ShouldThrowOnValue(
        string testName, string? value, int maxLength, string expectedExceptionTypeName)
    {
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfStringTooLong(value!, maxLength, nameof(value)),
            expected,
            "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> does not throw when the string length
    /// equals the maximum (boundary condition).
    /// </summary>
    [TestMethod]
    [DataRow("abcde", 5)]  // exactly at max
    [DataRow("", 0)]        // empty string at max 0
    public void ThrowIfStringTooLong_WhenLengthEqualsMaximum_ShouldNotThrow(string value, int maxLength) => ThrowHelper.ThrowIfStringTooLong(value, maxLength);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the string length exceeds the maximum.
    /// </summary>
    [TestMethod]
    [DataRow("abcdef", 5)]   // length 6 > max 5
    [DataRow("ab", 1)]       // length 2 > max 1
    [DataRow("x", 0)]        // length 1 > max 0
    public void ThrowIfStringTooLong_WhenLengthExceedsMaximum_ShouldThrowExactly(string value, int maxLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfStringTooLong(value, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> does not throw when the string length
    /// is strictly less than the maximum.
    /// </summary>
    [TestMethod]
    [DataRow("abc", 10)]
    [DataRow("", 1)]
    [DataRow("hello world", 100)]
    public void ThrowIfStringTooLong_WhenLengthIsBelowMaximum_ShouldNotThrow(string value, int maxLength) => ThrowHelper.ThrowIfStringTooLong(value, maxLength);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> throws <see cref="ArgumentNullException" />
    /// when the string value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStringTooLong_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringTooLong(null!, 10);
        });
    }

}
