// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniCommentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Behavioural and equality tests for the <see cref="IniComment" /> value type.
/// </summary>
[TestClass]
public sealed class IniCommentTests
{
    /// <summary>
    /// Verifies that a comment constructed with a valid prefix exposes its prefix, text, and line number.
    /// </summary>
    /// <param name="prefix">The introducing prefix character.</param>
    [TestMethod]
    [DataRow('#')]
    [DataRow(';')]
    public void Constructor_WhenPrefixIsValid_ShouldExposeComponents(char prefix)
    {
        IniComment comment = new(prefix, " note", 7);

        Assert.AreEqual((prefix, " note", 7), (comment.Prefix, comment.Text, comment.LineNumber));
    }

    /// <summary>
    /// Verifies that a prefix other than <c>'#'</c> or <c>';'</c> throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPrefixInvalid_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() => _ = new IniComment('/', "x"));

        Assert.AreEqual("prefix", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> text throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniComment('#', null!));

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="IniComment.ToString" /> concatenates the prefix and text.
    /// </summary>
    [TestMethod]
    public void ToString_ShouldConcatenatePrefixAndText()
    {
        Assert.AreEqual("# note", new IniComment('#', " note").ToString());
    }

    /// <summary>
    /// Verifies that two comments with identical components compare equal across every equality surface and hash alike.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAllComponentsMatch_ShouldReturnTrueAndShareHash()
    {
        IniComment a = new('#', "x", 3);
        IniComment b = new('#', "x", 3);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that comments differing in any single component compare unequal.
    /// </summary>
    /// <param name="prefix">The other comment's prefix.</param>
    /// <param name="text">The other comment's text.</param>
    /// <param name="line">The other comment's line number.</param>
    [TestMethod]
    [DataRow(';', "x", 3, DisplayName = "prefix differs")]
    [DataRow('#', "y", 3, DisplayName = "text differs")]
    [DataRow('#', "x", 4, DisplayName = "line differs")]
    public void Equals_WhenAnyComponentDiffers_ShouldReturnFalse(char prefix, string text, int line)
    {
        IniComment a = new('#', "x", 3);
        IniComment b = new(prefix, text, line);

        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a == b);
        Assert.IsTrue(a != b);
    }

    /// <summary>
    /// Verifies that comparing a comment to an instance of a different type returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOtherIsDifferentType_ShouldReturnFalse()
    {
        Assert.IsFalse(new IniComment('#', "x").Equals("not a comment"));
    }
}
