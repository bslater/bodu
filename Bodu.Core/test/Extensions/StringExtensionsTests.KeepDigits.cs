// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.KeepDigits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepDigits(string)" /> retains only digit characters.
    /// </summary>
    [TestMethod]
    public void KeepDigits_WhenInputContainsMixedCategories_ShouldRetainOnlyDigits()
    {
        Assert.AreEqual("12345", "(123) 456-789 - 5".KeepDigits()[..5]);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepDigits(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void KeepDigits_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.KeepDigits(null!));
    }
}
