// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.UnprefixLines.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.UnprefixLines(string, string)" /> strips the prefix from
    /// every prefixed line.
    /// </summary>
    [TestMethod]
    public void UnprefixLines_WhenAllLinesArePrefixed_ShouldStripPrefixFromEveryLine()
    {
        string actual = "// one\n// two".UnprefixLines("// ");

        Assert.AreEqual("one\ntwo", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.UnprefixLines(string, string)" /> leaves lines unchanged
    /// when they do not begin with the prefix.
    /// </summary>
    [TestMethod]
    public void UnprefixLines_WhenLineDoesNotBeginWithPrefix_ShouldLeaveLineUnchanged()
    {
        string actual = "// one\nxtwo".UnprefixLines("// ");

        Assert.AreEqual("one\nxtwo", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.UnprefixLines(string, string)" /> is the inverse of
    /// <see cref="StringExtensions.PrefixLines(string, string)" /> on a representative input.
    /// </summary>
    [TestMethod]
    public void UnprefixLines_AfterPrefixLines_ShouldRoundTrip()
    {
        const string input = "one\ntwo\r\nthree";

        string actual = input.PrefixLines("// ").UnprefixLines("// ");

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.UnprefixLines(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void UnprefixLines_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.UnprefixLines(null!, ">"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".UnprefixLines(null!));
    }
}
