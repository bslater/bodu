// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.KeepLetters.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepLetters(string)" /> retains only letters across ASCII
    /// and Unicode letter categories.
    /// </summary>
    [TestMethod]
    public void KeepLetters_WhenInputContainsMixedCategories_ShouldRetainOnlyLetters()
    {
        Assert.AreEqual("abcXYZéàü", "abc 123 XYZ! éàü".KeepLetters());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepLetters(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void KeepLetters_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.KeepLetters(null!));
    }
}
