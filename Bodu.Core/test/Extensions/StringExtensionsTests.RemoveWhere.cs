// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveWhere.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveWhere(string, Func{char, bool})" /> strips the
    /// characters where the predicate returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void RemoveWhere_WhenPredicateSupplied_ShouldRemoveMatchingCharacters()
    {
        var actual = "a1b2c3".RemoveWhere(char.IsDigit);

        Assert.AreEqual("abc", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveWhere(string, Func{char, bool})" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void RemoveWhere_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.RemoveWhere(null!, char.IsDigit));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".RemoveWhere(null!));
    }
}
