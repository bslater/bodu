// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocIndentTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Bodu.CodeStyle.XmlDocumentation.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Layout;

[TestClass]
public sealed class DocIndentTests
{
    /// <summary>
    /// Verifies that <see cref="DocIndent.Strip" /> throws when the trivia text is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Strip_WhenTriviaTextIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DocIndent.Strip(null!, "    ", "/// ");
        });

        Assert.AreEqual("triviaText", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DocIndent.Strip" /> throws when the base indent is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Strip_WhenBaseIndentIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DocIndent.Strip("/// foo\r\n", null!, "/// ");
        });

        Assert.AreEqual("baseIndent", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DocIndent.Strip" /> throws when the documentation prefix is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Strip_WhenDocumentationPrefixIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DocIndent.Strip("/// foo\r\n", "    ", null!);
        });

        Assert.AreEqual("documentationPrefix", ex.ParamName);
    }

    /// <summary>
    /// Verifies that stripping a single-line doc comment yields its prose content.
    /// </summary>
    [TestMethod]
    public void Strip_WhenSingleLine_ShouldReturnContentOnly()
    {
        var stripped = DocIndent.Strip("/// foo\r\n", string.Empty, "/// ");

        Assert.AreEqual("foo\n", stripped);
    }

    /// <summary>
    /// Verifies that stripping a multi-line doc comment yields content lines separated by <c>'\n'</c>.
    /// </summary>
    [TestMethod]
    public void Strip_WhenMultiLine_ShouldReturnContentLinesJoinedByLf()
    {
        var stripped = DocIndent.Strip(
            "/// foo\r\n" +
            "    /// bar\r\n",
            "    ",
            "/// ");

        Assert.AreEqual("foo\nbar\n", stripped);
    }

    /// <summary>
    /// Verifies that <see cref="DocIndent.Reapply" /> throws when the content list is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Reapply_WhenContentLinesIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DocIndent.Reapply(null!, string.Empty, "/// ", "\r\n");
        });

        Assert.AreEqual("contentLines", ex.ParamName);
    }

    /// <summary>
    /// Verifies that re-applying produces doc lines with the configured prefix and trailing line ending.
    /// </summary>
    [TestMethod]
    public void Reapply_WhenContentLinesGiven_ShouldEmitPrefixedLinesWithLineEnding()
    {
        var output = DocIndent.Reapply(new List<string> { "foo", "bar" }, "    ", "/// ", "\r\n");

        Assert.AreEqual(
            "/// foo\r\n" +
            "    /// bar\r\n",
            output);
    }

    /// <summary>
    /// Verifies that an empty content line is emitted with the prefix's leading characters (no trailing space)
    /// to honour the trim-trailing-whitespace convention.
    /// </summary>
    [TestMethod]
    public void Reapply_WhenContentLineIsEmpty_ShouldEmitPrefixWithoutTrailingSpace()
    {
        var output = DocIndent.Reapply(new List<string> { string.Empty }, string.Empty, "/// ", "\r\n");

        Assert.AreEqual("///\r\n", output);
    }

    /// <summary>
    /// Verifies that strip-then-reapply is a round-trip when the input is already canonical.
    /// </summary>
    [TestMethod]
    public void StripThenReapply_WhenCanonicalInput_ShouldRoundTrip()
    {
        var input =
            "/// foo\r\n" +
            "    /// bar\r\n";

        var stripped = DocIndent.Strip(input, "    ", "/// ");
        var lines = stripped.TrimEnd('\n').Split('\n');
        var output = DocIndent.Reapply(lines, "    ", "/// ", "\r\n");

        Assert.AreEqual(input, output);
    }
}
