// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrWhiteSpace.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> contract with explicit ParamName
    /// assertions: null → <see cref="ArgumentNullException" /> on "value"; empty or whitespace-only →
    /// <see cref="ArgumentException" /> on "value"; non-whitespace → pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The string passed to the guard.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null → ANE on value", null, "ArgumentNullException", "value")]
    [DataRow("empty → AE on value", "", "ArgumentException", "value")]
    [DataRow("spaces only → AE on value", "   ", "ArgumentException", "value")]
    [DataRow("tab only → AE on value", "\t", "ArgumentException", "value")]
    [DataRow("newline only → AE on value", "\n", "ArgumentException", "value")]
    [DataRow("non-whitespace → pass", "Valid", "", "")]
    [DataRow("leading whitespace then content → pass", "  trimmed", "", "")]
    [DataRow("internal space → pass", "middle space", "", "")]
    public void ThrowIfNullOrWhiteSpace_WhenInvokedWithVariousStrings_ShouldFollowContract(
        string testName, string? value, string expectedExceptionTypeName, string expectedParamName)
    {
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNullOrWhiteSpace(value!, nameof(value)),
            expected,
            param);
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
    public void ThrowIfNullOrWhiteSpace_WhenValueIsEmptyOrWhitespace_ShouldThrowArgumentException(string value)
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
    public void ThrowIfNullOrWhiteSpace_WhenValueIsNull_ShouldThrowArgumentNullException(string? value)
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
