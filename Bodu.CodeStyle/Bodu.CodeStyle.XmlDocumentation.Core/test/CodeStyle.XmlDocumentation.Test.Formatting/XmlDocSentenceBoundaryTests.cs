// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocSentenceBoundaryTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocSentenceBoundaryTests
{
    /// <summary>
    /// Verifies that a plain sentence boundary (a period followed by a space, after an ordinary word) is found.
    /// </summary>
    [TestMethod]
    public void FindFirstSentenceBoundary_WhenPlainBoundary_ShouldReturnPeriodIndex()
    {
        var content = "The value. More prose follows.";

        var index = XmlDocSentenceBoundary.FindFirstSentenceBoundary(content);

        Assert.AreEqual(content.IndexOf('.'), index);
    }

    /// <summary>
    /// Verifies that the abbreviation <c>e.g.</c> is not treated as a sentence boundary; the boundary is the
    /// next genuine sentence terminator.
    /// </summary>
    [TestMethod]
    public void FindFirstSentenceBoundary_WhenAbbreviationEg_ShouldSkipToRealBoundary()
    {
        var content = "Accepts a value, e.g. an index, used here. Then more.";

        var index = XmlDocSentenceBoundary.FindFirstSentenceBoundary(content);

        Assert.AreEqual(content.IndexOf("here.", System.StringComparison.Ordinal) + 4, index);
    }

    /// <summary>
    /// Verifies that a decimal or version number is not split at its internal periods.
    /// </summary>
    [TestMethod]
    public void FindFirstSentenceBoundary_WhenVersionNumber_ShouldNotSplitInsideIt()
    {
        var content = "Requires version 1.2.3 at minimum. Then more.";

        var index = XmlDocSentenceBoundary.FindFirstSentenceBoundary(content);

        Assert.AreEqual(content.IndexOf("minimum.", System.StringComparison.Ordinal) + 7, index);
    }

    /// <summary>
    /// Verifies that a dotted qualified name (containing internal periods) is not treated as a boundary.
    /// </summary>
    [TestMethod]
    public void FindFirstSentenceBoundary_WhenQualifiedNameOnly_ShouldReturnMinusOne()
    {
        var content = "The System.String. type here";

        var index = XmlDocSentenceBoundary.FindFirstSentenceBoundary(content);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that content with no sentence boundary returns -1 so the conservative rule does not fire.
    /// </summary>
    [TestMethod]
    public void FindFirstSentenceBoundary_WhenNoBoundary_ShouldReturnMinusOne()
    {
        var content = "A single clause with no terminator";

        var index = XmlDocSentenceBoundary.FindFirstSentenceBoundary(content);

        Assert.AreEqual(-1, index);
    }
}
