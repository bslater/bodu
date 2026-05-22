// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> contract with explicit ParamName
    /// assertions: null → <see cref="ArgumentNullException" /> on "value"; empty →
    /// <see cref="ArgumentException" /> on "value"; non-empty (including whitespace) → pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null → ANE on value", null, "ArgumentNullException", "value")]
    [DataRow("empty → AE on value", "", "ArgumentException", "value")]
    [DataRow("whitespace only → pass", "   ", "", "")]
    [DataRow("tab → pass", "\t", "", "")]
    [DataRow("single char → pass", "a", "", "")]
    [DataRow("typical string → pass", "test", "", "")]
    public void ThrowIfNullOrEmpty_WhenInvokedWithVariousStrings_ShouldFollowContract(
        string testName, string? value, string expectedExceptionTypeName, string expectedParamName)
    {
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNullOrEmpty(value!, nameof(value)),
            expected,
            param);
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
