// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrEmpty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for non-empty strings (including whitespace-only
    /// values; this guard permits whitespace by contract).
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    [TestMethod]
    [DataRow("whitespace only", "   ")]
    [DataRow("tab", "\t")]
    [DataRow("single char", "a")]
    [DataRow("typical string", "test")]
    public void ThrowIfNullOrEmpty_WhenValueIsNonEmpty_ShouldNotThrowAndReportNothing(string testName, string value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNullOrEmpty(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> throws <see cref="ArgumentNullException" />
    /// (when null) or <see cref="ArgumentException" /> (when empty), each with <c>ParamName == "value"</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DataRow("null", null, "ArgumentNullException")]
    [DataRow("empty", "", "ArgumentException")]
    public void ThrowIfNullOrEmpty_WhenValueIsRejected_ShouldThrowOnValue(
        string testName, string? value, string expectedExceptionTypeName)
    {
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNullOrEmpty(value!, nameof(value)),
            expected,
            "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> throws <see cref="ArgumentException" />
    /// when the value is an empty string.
    /// </summary>
    [DataRow("")]
    [TestMethod]
    public void ThrowIfNullOrEmpty_WhenValueIsEmpty_ShouldThrowExactly(string value)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNullOrEmpty(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> does not throw for any non-empty string,
    /// including whitespace-only values. Callers that must also reject whitespace should use
    /// <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> instead.
    /// </summary>
    [DataRow("a")]
    [DataRow("test")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [TestMethod]
    public void ThrowIfNullOrEmpty_WhenValueIsNonEmpty_ShouldNotThrow(string value) => ThrowHelper.ThrowIfNullOrEmpty(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> throws <see cref="ArgumentNullException" />
    /// when the value is <see langword="null" />.
    /// </summary>
    [DataRow(null)]
    [TestMethod]
    public void ThrowIfNullOrEmpty_WhenValueIsNull_ShouldThrowExactly(string? value)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNullOrEmpty(value!);
        });
    }

}
