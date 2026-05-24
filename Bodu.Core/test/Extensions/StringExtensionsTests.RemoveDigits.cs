// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveDigits.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveDigits(string)" /> strips every digit character.
    /// </summary>
    [TestMethod]
    public void RemoveDigits_WhenInputContainsDigits_ShouldRemoveThem()
    {
        Assert.AreEqual("abcXYZ", "a1b2c3X4Y5Z6".RemoveDigits());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveDigits(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveDigits_WhenInputIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.RemoveDigits(null!));
    }
}
