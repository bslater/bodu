// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrWhiteSpace.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — when the value contains at least one non-whitespace
    /// character.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    [TestMethod]
    [DataRow("non-whitespace", "Valid")]
    [DataRow("leading whitespace then content", "  trimmed")]
    [DataRow("internal space", "middle space")]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsNonWhitespace_ShouldNotThrowAndReportNothing(string testName, string value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNullOrWhiteSpace(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> throws
    /// <see cref="ArgumentNullException" /> (when null) or <see cref="ArgumentException" /> (when empty or
    /// whitespace-only), each with <c>ParamName == "value"</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DataRow("null", null, "ArgumentNullException")]
    [DataRow("empty", "", "ArgumentException")]
    [DataRow("spaces only", "   ", "ArgumentException")]
    [DataRow("tab only", "\t", "ArgumentException")]
    [DataRow("newline only", "\n", "ArgumentException")]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsRejected_ShouldThrowOnValue(
        string testName, string? value, string expectedExceptionTypeName)
    {
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNullOrWhiteSpace(value!, nameof(value)),
            expected,
            "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> throws <see cref="ArgumentException" />
    /// when the value is empty or contains only whitespace characters.
    /// </summary>
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [TestMethod]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsEmptyOrWhitespace_ShouldThrowExactly(string value)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNullOrWhiteSpace(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> throws <see cref="ArgumentNullException" />
    /// when the value is <see langword="null" />.
    /// </summary>
    [DataRow(null)]
    [TestMethod]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsNull_ShouldThrowExactly(string? value)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNullOrWhiteSpace(value!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> does not throw when the value contains
    /// at least one non-whitespace character.
    /// </summary>
    [DataRow("Valid")]
    [DataRow("x")]
    [DataRow("  trimmed")]
    [DataRow("middle space")]
    [TestMethod]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsValid_ShouldNotThrow(string value) => ThrowHelper.ThrowIfNullOrWhiteSpace(value);

}
