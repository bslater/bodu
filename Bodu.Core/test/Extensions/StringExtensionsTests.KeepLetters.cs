// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.KeepLetters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepLetters(string)" /> retains only letters across ASCII
    /// and Unicode letter categories.
    /// </summary>
    [TestMethod]
    public void KeepLetters_WhenInputContainsMixedCategories_ShouldRetainOnlyLetters() => Assert.AreEqual("abcXYZéàü", "abc 123 XYZ! éàü".KeepLetters());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepLetters(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void KeepLetters_WhenInputIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.KeepLetters(null!));
}
