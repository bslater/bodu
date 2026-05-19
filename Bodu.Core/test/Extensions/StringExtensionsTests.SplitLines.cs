// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.SplitLines.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

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
        string value = "alpha\nbeta\r\ngamma\rdelta";

        string[] actual = value.SplitLines().ToArray();

        CollectionAssert.AreEqual(new[] { "alpha", "beta", "gamma", "delta" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> does not emit a final empty
    /// line when the input ends with a terminator.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenInputEndsWithNewline_ShouldNotEmitTrailingEmptyLine()
    {
        string value = "alpha\nbeta\n";

        string[] actual = value.SplitLines().ToArray();

        CollectionAssert.AreEqual(new[] { "alpha", "beta" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> with
    /// <c>removeEmptyLines: true</c> filters out empty entries that arise from consecutive terminators.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenRemoveEmptyLinesIsTrue_ShouldSkipEmptyEntries()
    {
        string value = "alpha\n\nbeta\r\n\r\ngamma";

        string[] actual = value.SplitLines(removeEmptyLines: true).ToArray();

        CollectionAssert.AreEqual(new[] { "alpha", "beta", "gamma" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> returns an empty sequence for
    /// an empty input string.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenInputIsEmpty_ShouldReturnEmptySequence()
    {
        Assert.AreEqual(0, string.Empty.SplitLines().Count());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SplitLines(string, bool)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SplitLines_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.SplitLines(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
