// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveControlCharacters.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveControlCharacters(string)" /> strips tab, CR, LF, and
    /// other control characters.
    /// </summary>
    [TestMethod]
    public void RemoveControlCharacters_WhenInputContainsControlChars_ShouldStripThem() => Assert.AreEqual("helloworld", "hello\t\r\nworld".RemoveControlCharacters());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveControlCharacters(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveControlCharacters_WhenInputIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.RemoveControlCharacters(null!));
}
