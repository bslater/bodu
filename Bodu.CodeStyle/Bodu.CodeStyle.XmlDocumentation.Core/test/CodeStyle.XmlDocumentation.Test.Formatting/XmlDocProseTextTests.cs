// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocProseTextTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
