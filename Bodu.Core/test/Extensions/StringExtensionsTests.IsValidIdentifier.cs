// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.IsValidIdentifier.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="IsValidIdentifier_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetIsValidIdentifierCases() =>
    [
        ["", false],
        ["_", true],
        ["a", true],
        ["A", true],
        ["abc", true],
        ["_abc", true],
        ["abc123", true],
        ["abc_123", true],
        ["_123", true],
        ["123abc", false],
        ["1", false],
        [" abc", false],
        ["abc def", false],
        ["abc-def", false],
        ["abc.def", false],
        ["héllo", true],
        ["café", true],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsValidIdentifier(string)" /> returns the expected value for
    /// each canonical input.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">Whether the candidate is expected to be a valid identifier.</param>
    [TestMethod]
    [DynamicData(nameof(GetIsValidIdentifierCases))]
    public void IsValidIdentifier_WhenInvoked_ShouldReturnExpected(string value, bool expected) => Assert.AreEqual(expected, value.IsValidIdentifier());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsValidIdentifier(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsValidIdentifier_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.IsValidIdentifier(null!);
        });
    }
}
