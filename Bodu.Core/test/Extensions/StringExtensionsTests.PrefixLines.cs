// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.PrefixLines.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.PrefixLines(string, string)" /> prepends the supplied prefix
    /// to every line of a multi-line LF input.
    /// </summary>
    [TestMethod]
    public void PrefixLines_WhenInputUsesLf_ShouldPrependPrefixToEveryLine()
    {
        string actual = "one\ntwo".PrefixLines("// ");

        Assert.AreEqual("// one\n// two", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.PrefixLines(string, string)" /> preserves CRLF line
    /// boundaries.
    /// </summary>
    [TestMethod]
    public void PrefixLines_WhenInputUsesCrLf_ShouldPreserveLineBoundaries()
    {
        string actual = "one\r\ntwo".PrefixLines("> ");

        Assert.AreEqual("> one\r\n> two", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.PrefixLines(string, string)" /> returns the input unchanged
    /// when the prefix is empty.
    /// </summary>
    [TestMethod]
    public void PrefixLines_WhenPrefixIsEmpty_ShouldReturnInputUnchanged() => Assert.AreEqual("one\ntwo", "one\ntwo".PrefixLines(string.Empty));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.PrefixLines(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void PrefixLines_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.PrefixLines(null!, ">"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".PrefixLines(null!));
    }
}
