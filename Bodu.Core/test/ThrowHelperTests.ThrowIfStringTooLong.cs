// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringTooLong.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfStringTooLong" /> contract with explicit ParamName
    /// assertions: null → <see cref="ArgumentNullException" /> on "value"; length > max →
    /// <see cref="ArgumentOutOfRangeException" /> on "value"; length &lt;= max → pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard, or <see langword="null" />.</param>
    /// <param name="maxLength">Inclusive maximum length.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null → ANE on value", null, 10, "ArgumentNullException", "value")]
    [DataRow("longer than max → AOORE on value", "abcdef", 5, "ArgumentOutOfRangeException", "value")]
    [DataRow("non-empty vs max=0 → AOORE on value", "x", 0, "ArgumentOutOfRangeException", "value")]
    [DataRow("exactly at max → pass", "abcde", 5, "", "")]
    [DataRow("empty vs max=0 → pass", "", 0, "", "")]
    [DataRow("below max → pass", "abc", 10, "", "")]
    public void ThrowIfStringTooLong_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, string? value, int maxLength, string expectedExceptionTypeName, string expectedParamName)
    {
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfStringTooLong(value!, maxLength, nameof(value)),
            expected,
            param);
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
