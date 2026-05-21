// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemovePunctuation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemovePunctuation(string)" /> strips ASCII and Unicode
    /// punctuation while preserving letters, digits, and whitespace.
    /// </summary>
    [TestMethod]
    public void RemovePunctuation_WhenInputContainsPunctuation_ShouldStripIt()
    {
        Assert.AreEqual("Hello World  its", "Hello, World — it's!?".RemovePunctuation());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemovePunctuation(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemovePunctuation_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.RemovePunctuation(null!));
    }
}
