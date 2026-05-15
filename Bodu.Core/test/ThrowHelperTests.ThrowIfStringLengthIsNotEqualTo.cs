// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringLengthIsNotEqualTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> contract with explicit
    /// ParamName assertions: null → <see cref="ArgumentNullException" /> on "value"; length mismatch →
    /// <see cref="ArgumentException" /> on "value"; exact match → pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard, or <see langword="null" />.</param>
    /// <param name="expectedLength">The required exact length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null → ANE on value", null, 5, "ArgumentNullException", "value")]
    [DataRow("shorter → AE on value", "abc", 5, "ArgumentException", "value")]
    [DataRow("longer → AE on value", "abcdef", 5, "ArgumentException", "value")]
    [DataRow("empty vs expected 1 → AE on value", "", 1, "ArgumentException", "value")]
    [DataRow("exact match → pass", "abcde", 5, "", "")]
    [DataRow("empty vs 0 → pass", "", 0, "", "")]
    public void ThrowIfStringLengthIsNotEqualTo_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, string? value, int expectedLength, string expectedExceptionTypeName, string expectedParamName)
    {
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfStringLengthIsNotEqualTo(value!, expectedLength, nameof(value)),
            expected,
            param);
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
    public void ThrowIfStringLengthIsNotEqualTo_WhenLengthDiffers_ShouldThrowArgumentException(string value, int expectedLength)
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
    public void ThrowIfStringLengthIsNotEqualTo_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthIsNotEqualTo(null!, 5);
        });
    }

}
