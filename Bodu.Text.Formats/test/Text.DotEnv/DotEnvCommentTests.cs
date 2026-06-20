// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvCommentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Behavioural and equality tests for the <see cref="DotEnvComment" /> value type.
/// </summary>
[TestClass]
public sealed class DotEnvCommentTests
{
    /// <summary>
    /// Verifies that a comment constructed with the <c>'#'</c> prefix exposes its prefix, text, and line number.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPrefixIsValid_ShouldExposeComponents()
    {
        DotEnvComment comment = new('#', " note", 7);

        Assert.AreEqual(('#', " note", 7), (comment.Prefix, comment.Text, comment.LineNumber));
    }

    /// <summary>
    /// Verifies that a prefix other than <c>'#'</c> throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPrefixInvalid_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => _ = new DotEnvComment(';', "x"));

        Assert.AreEqual("prefix", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> text throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new DotEnvComment('#', null!));

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvComment.ToString" /> concatenates the prefix and text.
    /// </summary>
    [TestMethod]
    public void ToString_ShouldConcatenatePrefixAndText()
    {
        Assert.AreEqual("# note", new DotEnvComment('#', " note").ToString());
    }

    /// <summary>
    /// Verifies that two comments with identical components compare equal across every equality surface and hash alike.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAllComponentsMatch_ShouldReturnTrueAndShareHash()
    {
        DotEnvComment a = new('#', "x", 3);
        DotEnvComment b = new('#', "x", 3);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that comments differing in text or line number compare unequal.
    /// </summary>
    /// <param name="text">The other comment's text.</param>
    /// <param name="line">The other comment's line number.</param>
    [TestMethod]
    [DataRow("y", 3, DisplayName = "text differs")]
    [DataRow("x", 4, DisplayName = "line differs")]
    public void Equals_WhenAnyComponentDiffers_ShouldReturnFalse(string text, int line)
    {
        DotEnvComment a = new('#', "x", 3);
        DotEnvComment b = new('#', text, line);

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
        Assert.IsFalse(new DotEnvComment('#', "x").Equals("not a comment"));
    }
}
