// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.KeepLettersAndDigits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepLettersAndDigits(string)" /> retains letters and digits
    /// while removing punctuation and whitespace.
    /// </summary>
    [TestMethod]
    public void KeepLettersAndDigits_WhenInputContainsMixedCategories_ShouldRetainAlphanumericOnly()
    {
        Assert.AreEqual("abc123XYZ", "abc 123 XYZ!".KeepLettersAndDigits());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepLettersAndDigits(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void KeepLettersAndDigits_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.KeepLettersAndDigits(null!));
    }
}
