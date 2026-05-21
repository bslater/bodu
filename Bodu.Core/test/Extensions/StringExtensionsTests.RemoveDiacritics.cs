// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveDiacritics.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative <c>(input, expected)</c> tuples for
    /// <see cref="RemoveDiacritics_WhenInvoked_ShouldStripCombiningMarks" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetRemoveDiacriticsCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["café", "cafe"],
        ["naïve", "naive"],
        ["façade", "facade"],
        ["jalapeño", "jalapeno"],
        ["über", "uber"],
        ["Crème Brûlée", "Creme Brulee"],
        ["Příliš žluťoučký", "Prilis zlutoucky"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveDiacritics(string)" /> strips combining marks while
    /// preserving the base characters.
    /// </summary>
    /// <param name="value">The string to strip.</param>
    /// <param name="expected">The expected stripped form.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetRemoveDiacriticsCases), DynamicDataSourceType.Method)]
    public void RemoveDiacritics_WhenInvoked_ShouldStripCombiningMarks(string value, string expected)
    {
        Assert.AreEqual(expected, value.RemoveDiacritics());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveDiacritics(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveDiacritics_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.RemoveDiacritics(null!);
        });
    }
}
