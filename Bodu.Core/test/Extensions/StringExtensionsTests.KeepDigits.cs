// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.KeepDigits.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    public void KeepDigits_WhenInputIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.KeepDigits(null!));
    }
}
