// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Outdent.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.Outdent(string, int, char)" /> removes the requested number
    /// of leading spaces from every line.
    /// </summary>
    [TestMethod]
    public void Outdent_WhenAllLinesAreFullyIndented_ShouldStripIndentFromEveryLine()
    {
        var actual = "    one\n    two".Outdent(2);

        Assert.AreEqual("  one\n  two", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Outdent(string, int, char)" /> removes fewer characters than
    /// requested when a line is shorter, rather than throwing.
    /// </summary>
    [TestMethod]
    public void Outdent_WhenLineHasFewerLeadingChars_ShouldStripWhatIsThere()
    {
        var actual = "  one\nx".Outdent(4);

        Assert.AreEqual("one\nx", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Outdent(string, int, char)" /> is the inverse of
    /// <see cref="StringExtensions.Indent(string, int, char)" /> on a representative input.
    /// </summary>
    [TestMethod]
    public void Outdent_AfterIndent_ShouldRoundTrip()
    {
        const string input = "one\ntwo\r\nthree";

        var actual = input.Indent(2).Outdent(2);

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Outdent(string, int, char)" /> respects a custom indent
    /// character.
    /// </summary>
    [TestMethod]
    public void Outdent_WhenCustomIndentCharSupplied_ShouldUseIt()
    {
        var actual = "\t\tone\n\t\ttwo".Outdent(2, '\t');

        Assert.AreEqual("one\ntwo", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Outdent(string, int, char)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Outdent_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.Outdent(null!, 2);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Outdent(string, int, char)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>count</c> is negative.
    /// </summary>
    [TestMethod]
    public void Outdent_WhenCountIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = "hello".Outdent(-1);
        });
    }
}
