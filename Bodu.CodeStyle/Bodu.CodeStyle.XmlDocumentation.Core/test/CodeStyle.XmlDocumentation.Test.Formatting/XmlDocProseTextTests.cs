// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocProseTextTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocProseTextTests
{
    /// <summary>
    /// Verifies that single-line prose is returned with only its surrounding whitespace trimmed.
    /// </summary>
    [TestMethod]
    public void Canonicalize_WhenSingleLine_ShouldTrimOnly()
    {
        Assert.AreEqual("The element type.", XmlDocProseText.Canonicalize("The element type."));
    }

    /// <summary>
    /// Verifies that a leading indentation, the <c>///</c> prefix, and one following space are stripped down to
    /// the logical content.
    /// </summary>
    [TestMethod]
    public void StripPrefix_WhenStandardDocLine_ShouldRemoveIndentPrefixAndOneSpace()
    {
        Assert.AreEqual("The summary.", XmlDocProseText.StripPrefix("        /// The summary."));
    }

    /// <summary>
    /// Verifies that a <c>///</c> prefix with no following space is stripped without consuming a content
    /// character.
    /// </summary>
    [TestMethod]
    public void StripPrefix_WhenNoSpaceAfterPrefix_ShouldRemovePrefixOnly()
    {
        Assert.AreEqual("The summary.", XmlDocProseText.StripPrefix("        ///The summary."));
    }

    /// <summary>
    /// Verifies that only a single space following the <c>///</c> prefix is consumed, so deliberate body
    /// indentation is preserved.
    /// </summary>
    [TestMethod]
    public void StripPrefix_WhenMultipleSpacesAfterPrefix_ShouldConsumeOnlyOne()
    {
        Assert.AreEqual("  indented", XmlDocProseText.StripPrefix("    ///   indented"));
    }

    /// <summary>
    /// Verifies that trailing whitespace is preserved (the primitive trims only the leading prefix, never the
    /// end of the line).
    /// </summary>
    [TestMethod]
    public void StripPrefix_WhenTrailingWhitespace_ShouldPreserveIt()
    {
        Assert.AreEqual("text   ", XmlDocProseText.StripPrefix("/// text   "));
    }

    /// <summary>
    /// Verifies that a line without a <c>///</c> prefix still has its leading indentation removed.
    /// </summary>
    [TestMethod]
    public void StripPrefix_WhenNoDocPrefix_ShouldStripLeadingIndentOnly()
    {
        Assert.AreEqual("plain text", XmlDocProseText.StripPrefix("    plain text"));
    }

    /// <summary>
    /// Verifies that single-line content is returned as a single logical body line.
    /// </summary>
    [TestMethod]
    public void ToLogicalBodyLines_WhenSingleLine_ShouldReturnOneLine()
    {
        CollectionAssert.AreEqual(
            new List<string> { "var x = 5;" },
            ToList(XmlDocProseText.ToLogicalBodyLines("var x = 5;")));
    }

    /// <summary>
    /// Verifies that multi-line content with continuation <c>///</c> prefixes is reduced to its logical body
    /// lines with the prefixes and per-line trailing whitespace removed.
    /// </summary>
    [TestMethod]
    public void ToLogicalBodyLines_WhenMultiLineWithDocPrefixes_ShouldStripEachLine()
    {
        var raw = "foo   \r\n        /// bar\r\n        /// baz\t";

        CollectionAssert.AreEqual(
            new List<string> { "foo", "bar", "baz" },
            ToList(XmlDocProseText.ToLogicalBodyLines(raw)));
    }

    /// <summary>
    /// Verifies that leading and trailing blank lines are dropped while interior blank lines are preserved, so
    /// the caller alone controls the surrounding layout.
    /// </summary>
    [TestMethod]
    public void ToLogicalBodyLines_WhenSurroundingBlankLines_ShouldTrimEndsButKeepInterior()
    {
        var raw = "\r\n        /// a\r\n        ///\r\n        /// b\r\n        ";

        CollectionAssert.AreEqual(
            new List<string> { "a", string.Empty, "b" },
            ToList(XmlDocProseText.ToLogicalBodyLines(raw)));
    }

    /// <summary>
    /// Verifies that content consisting only of blank lines yields an empty result.
    /// </summary>
    [TestMethod]
    public void ToLogicalBodyLines_WhenAllBlank_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, XmlDocProseText.ToLogicalBodyLines("\r\n        ///\r\n        ").Count);
    }

    private static List<string> ToList(IReadOnlyList<string> source) => new(source);

    /// <summary>
    /// Verifies that multi-line prose with continuation <c>///</c> doc-comment prefixes collapses to a single
    /// canonical line, matching what the formatter would render.
    /// </summary>
    [TestMethod]
    public void Canonicalize_WhenMultiLineWithDocPrefixes_ShouldCollapseToSingleLine()
    {
        var raw = "The element\r\n        /// type used\r\n        /// across the buffer.";

        Assert.AreEqual("The element type used across the buffer.", XmlDocProseText.Canonicalize(raw));
    }

    /// <summary>
    /// Verifies that runs of interior whitespace collapse to a single space.
    /// </summary>
    [TestMethod]
    public void Canonicalize_WhenInteriorWhitespaceRuns_ShouldCollapse()
    {
        Assert.AreEqual("a b c", XmlDocProseText.Canonicalize("a    b\tc"));
    }
}
