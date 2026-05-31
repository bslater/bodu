// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringLengthIsNotEqualTo.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — when the string length matches the expected length
    /// exactly.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="expectedLength">The required exact length.</param>
    [TestMethod]
    [DataRow("exact match", "abcde", 5)]
    [DataRow("empty vs 0", "", 0)]
    public void ThrowIfStringLengthIsNotEqualTo_WhenValueMatches_ShouldNotThrowAndReportNothing(
        string testName, string value, int expectedLength) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfStringLengthIsNotEqualTo(value, expectedLength, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> throws
    /// <see cref="ArgumentNullException" /> (when null) or <see cref="ArgumentException" /> (when length
    /// differs), each with <c>ParamName == "value"</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="expectedLength">The required exact length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DataRow("null", null, 5, "ArgumentNullException")]
    [DataRow("shorter", "abc", 5, "ArgumentException")]
    [DataRow("longer", "abcdef", 5, "ArgumentException")]
    [DataRow("empty vs expected 1", "", 1, "ArgumentException")]
    public void ThrowIfStringLengthIsNotEqualTo_WhenValueIsRejected_ShouldThrowOnValue(
        string testName, string? value, int expectedLength, string expectedExceptionTypeName)
    {
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfStringLengthIsNotEqualTo(value!, expectedLength, nameof(value)),
            expected,
            "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> throws
    /// <see cref="ArgumentException" /> when the string length does not equal the expected length.
    /// </summary>
    [TestMethod]
    [DataRow("abc", 5)]    // shorter than expected
    [DataRow("abcdef", 5)] // longer than expected
    [DataRow("", 1)]       // empty but 1 expected
    [DataRow("ab", 0)]     // non-empty but 0 expected
    public void ThrowIfStringLengthIsNotEqualTo_WhenLengthDiffers_ShouldThrowExactly(string value, int expectedLength)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthIsNotEqualTo(value, expectedLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> does not throw when the
    /// string length exactly matches the expected length.
    /// </summary>
    [TestMethod]
    [DataRow("abcde", 5)]
    [DataRow("", 0)]
    [DataRow("x", 1)]
    [DataRow("US", 2)]          // typical country code length
    [DataRow("AAPLUSS00000", 12)] // ISIN-length string
    public void ThrowIfStringLengthIsNotEqualTo_WhenLengthMatches_ShouldNotThrow(string value, int expectedLength) => ThrowHelper.ThrowIfStringLengthIsNotEqualTo(value, expectedLength);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> throws
    /// <see cref="ArgumentNullException" /> when the string value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStringLengthIsNotEqualTo_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthIsNotEqualTo(null!, 5);
        });
    }

}
