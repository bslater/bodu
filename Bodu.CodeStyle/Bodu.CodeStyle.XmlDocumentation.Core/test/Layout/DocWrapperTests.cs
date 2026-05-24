// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocWrapperTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Bodu.CodeStyle.XmlDocumentation.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Layout;

[TestClass]
public sealed class DocWrapperTests
{
    /// <summary>
    /// Verifies that <see cref="DocWrapper.Wrap" /> throws when the atoms list is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenAtomsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DocWrapper.Wrap(null!, 80).ToList();
        });

        Assert.AreEqual("atoms", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DocWrapper.Wrap" /> throws when the content budget is non-positive.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenContentBudgetIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DocWrapper.Wrap(new List<string> { "foo" }, 0).ToList();
        });

        Assert.AreEqual("contentBudget", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty atoms list produces no output lines.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenAtomsIsEmpty_ShouldReturnNoLines()
    {
        var result = DocWrapper.Wrap(new List<string>(), 80).ToList();

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that short content fits on one line.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenContentFits_ShouldReturnSingleLine()
    {
        var result = DocWrapper.Wrap(new List<string> { "foo", " ", "bar" }, 80).ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("foo bar", result[0]);
    }

    /// <summary>
    /// Verifies that long content wraps at whitespace boundaries.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenContentExceedsBudget_ShouldWrapAtWhitespace()
    {
        var result = DocWrapper.Wrap(new List<string> { "foo", " ", "bar", " ", "baz" }, 7).ToList();

        Assert.IsTrue(result.Count > 1);
        foreach (var line in result)
        {
            // Each emitted line should be either a single atom or fit the budget.
            Assert.IsTrue(line.Length <= 7 || !line.Contains(' '));
        }
    }

    /// <summary>
    /// Verifies that a single atom longer than the budget is emitted on its own line rather than being split.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenSingleAtomExceedsBudget_ShouldEmitOnItsOwnLine()
    {
        var longAtom = "Aabcdefghijklmnopqrstuvwxyz";
        var result = DocWrapper.Wrap(new List<string> { longAtom }, 10).ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(longAtom, result[0]);
    }

    /// <summary>
    /// Verifies that whitespace atoms at the boundary do not appear in the output.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenWhitespaceFallsOnWrapBoundary_ShouldBeDroppedFromOutput()
    {
        var result = DocWrapper.Wrap(new List<string> { "aaaa", " ", "bbbb" }, 4).ToList();

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("aaaa", result[0]);
        Assert.AreEqual("bbbb", result[1]);
    }
}
