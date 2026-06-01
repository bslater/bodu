// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.EqualsOrdinalIgnoreCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="EqualsOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetEqualsOrdinalIgnoreCaseCases() =>
    [
        [null, null, true],
        [null, "", false],
        ["", null, false],
        ["", "", true],
        ["hello", "hello", true],
        ["hello", "HELLO", true],
        ["hello", "HeLLo", true],
        ["hello", "world", false],
        ["café", "CAFÉ", true],
        ["hello", "hello ", false],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EqualsOrdinalIgnoreCase(string?, string?)" /> matches
    /// <c>string.Equals</c> with <c>StringComparison.OrdinalIgnoreCase</c> across null, empty, and
    /// case-varied inputs.
    /// </summary>
    /// <param name="value">The first string.</param>
    /// <param name="other">The second string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetEqualsOrdinalIgnoreCaseCases))]
    public void EqualsOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected(string? value, string? other, bool expected) => Assert.AreEqual(expected, value.EqualsOrdinalIgnoreCase(other));
}
