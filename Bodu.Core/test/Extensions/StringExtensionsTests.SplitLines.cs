// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.SplitLines.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> splits on every CR, LF, and
    /// CRLF boundary.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenMixedLineEndings_ShouldSplitOnEachBoundary()
    {
        var value = "alpha\nbeta\r\ngamma\rdelta";

        var actual = value.SplitLines().ToArray();

        CollectionAssert.AreEqual(new[] { "alpha", "beta", "gamma", "delta" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> does not emit a final empty
    /// line when the input ends with a terminator.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenInputEndsWithNewline_ShouldNotEmitTrailingEmptyLine()
    {
        var value = "alpha\nbeta\n";

        var actual = value.SplitLines().ToArray();

        CollectionAssert.AreEqual(new[] { "alpha", "beta" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> with
    /// <c>removeEmptyLines: true</c> filters out empty entries that arise from consecutive terminators.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenRemoveEmptyLinesIsTrue_ShouldSkipEmptyEntries()
    {
        var value = "alpha\n\nbeta\r\n\r\ngamma";

        var actual = value.SplitLines(removeEmptyLines: true).ToArray();

        CollectionAssert.AreEqual(new[] { "alpha", "beta", "gamma" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> returns an empty sequence for
    /// an empty input string.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenInputIsEmpty_ShouldReturnEmptySequence() => Assert.IsEmpty(string.Empty.SplitLines());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenInputIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.SplitLines(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
