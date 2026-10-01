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

    /// <summary>
    /// Verifies that when the atom that would open a continuation line is one Markdown reads as the start of a block
    /// (a list bullet, an ordered list starting at one, a heading, a block quote, or a setext underline), the break
    /// moves before the preceding word, so the atom never opens a line that DocFX would render as a list or heading.
    /// </summary>
    /// <param name="marker">The atom Markdown reads as a block marker at the start of a line.</param>
    [TestMethod]
    [DataRow("-")]
    [DataRow("+")]
    [DataRow("*")]
    [DataRow("#")]
    [DataRow("###")]
    [DataRow("&gt;")]
    [DataRow("&gt;=")]
    [DataRow("1.")]
    [DataRow("1)")]
    [DataRow("=")]
    [DataRow("--")]
    public void Wrap_WhenOverflowAtomIsMarkdownBlockMarker_ShouldCarryPrecedingWordOntoNextLine(string marker)
    {
        var result = DocWrapper.Wrap(new List<string> { "aaaa", " ", "bbbb", " ", marker, " ", "cccc" }, 9).ToList();

        CollectionAssert.AreEqual(new[] { "aaaa", "bbbb " + marker, "cccc" }, result);
    }

    /// <summary>
    /// Verifies that an atom that only resembles a block marker (an ordered list number other than one, a signed
    /// number, an arrow, or a hash followed by text) opens a continuation line as any other word does.
    /// </summary>
    /// <param name="atom">The atom that Markdown does not read as a block marker.</param>
    [TestMethod]
    [DataRow("2.")]
    [DataRow("10)")]
    [DataRow("-1")]
    [DataRow("->")]
    [DataRow("#1")]
    public void Wrap_WhenOverflowAtomOnlyResemblesMarker_ShouldBreakBeforeIt(string atom)
    {
        var result = DocWrapper.Wrap(new List<string> { "aaaa", " ", "bbbb", " ", atom, " ", "cccc" }, 9).ToList();

        CollectionAssert.AreEqual(new[] { "aaaa bbbb", atom + " cccc" }, result);
    }

    /// <summary>
    /// Verifies that a block marker that overflows a line holding a single word stays on that line, over budget, since
    /// there is no earlier break to move to.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenMarkerFollowsTheOnlyWordOnTheLine_ShouldKeepMarkerOnThatLine()
    {
        var result = DocWrapper.Wrap(new List<string> { "aaaaaaaa", " ", "-", " ", "bbbb" }, 8).ToList();

        CollectionAssert.AreEqual(new[] { "aaaaaaaa -", "bbbb" }, result);
    }

    /// <summary>
    /// Verifies that when the word the break would carry is itself a block marker, the break moves back to the word
    /// before it, so neither marker opens a line.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenCarriedWordIsItselfAMarker_ShouldCarryTheWordBeforeIt()
    {
        var result = DocWrapper.Wrap(new List<string> { "aaaa", " ", "bbbb", " ", "+", " ", "-", " ", "cccc" }, 11).ToList();

        CollectionAssert.AreEqual(new[] { "aaaa", "bbbb + -", "cccc" }, result);
    }

    /// <summary>
    /// Verifies that a block marker the author placed at the start of the text is left where it is: only a break the
    /// wrapper chooses is moved.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenTextStartsWithMarker_ShouldLeaveItInPlace()
    {
        var result = DocWrapper.Wrap(new List<string> { "-", " ", "aaaa" }, 80).ToList();

        CollectionAssert.AreEqual(new[] { "- aaaa" }, result);
    }
}
