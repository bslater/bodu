// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Indent.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.Indent(string, int, char)" /> prepends spaces to every line
    /// when the input uses LF line endings.
    /// </summary>
    [TestMethod]
    public void Indent_WhenInputUsesLf_ShouldPrependIndentToEveryLine()
    {
        var actual = "one\ntwo\nthree".Indent(2);

        Assert.AreEqual("  one\n  two\n  three", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Indent(string, int, char)" /> respects CRLF line endings and
    /// does not split a CR/LF pair across the line boundary.
    /// </summary>
    [TestMethod]
    public void Indent_WhenInputUsesCrLf_ShouldPrependIndentToEveryLine()
    {
        var actual = "one\r\ntwo".Indent(2);

        Assert.AreEqual("  one\r\n  two", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Indent(string, int, char)" /> accepts a custom indent
    /// character.
    /// </summary>
    [TestMethod]
    public void Indent_WhenCustomIndentCharSupplied_ShouldUseIt()
    {
        var actual = "a\nb".Indent(3, '>');

        Assert.AreEqual(">>>a\n>>>b", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Indent(string, int, char)" /> returns the input unchanged
    /// when <c>count</c> is zero.
    /// </summary>
    [TestMethod]
    public void Indent_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        Assert.AreEqual("one\ntwo", "one\ntwo".Indent(0));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Indent(string, int, char)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indent_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.Indent(null!, 2);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Indent(string, int, char)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>count</c> is negative.
    /// </summary>
    [TestMethod]
    public void Indent_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = "hello".Indent(-1);
        });
    }
}
