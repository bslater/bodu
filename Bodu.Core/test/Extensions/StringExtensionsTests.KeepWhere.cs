// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.KeepWhere.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepWhere(string, Func{char, bool})" /> retains only the
    /// characters where the predicate returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void KeepWhere_WhenPredicateSupplied_ShouldRetainMatchingCharacters()
    {
        var actual = "a1b2c3".KeepWhere(char.IsDigit);

        Assert.AreEqual("123", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepWhere(string, Func{char, bool})" /> returns the
    /// original instance when every character matches the predicate.
    /// </summary>
    [TestMethod]
    public void KeepWhere_WhenAllCharactersMatch_ShouldReturnSameInstance()
    {
        var value = "abc";

        var actual = value.KeepWhere(char.IsLetter);

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepWhere(string, Func{char, bool})" /> returns
    /// <see cref="string.Empty" /> when no character matches.
    /// </summary>
    [TestMethod]
    public void KeepWhere_WhenNoCharacterMatches_ShouldReturnEmptyString()
    {
        var actual = "abc".KeepWhere(char.IsDigit);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.KeepWhere(string, Func{char, bool})" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void KeepWhere_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.KeepWhere(null!, char.IsDigit));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".KeepWhere(null!));
    }
}
