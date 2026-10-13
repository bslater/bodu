// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.LineStart.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8LineCursorTests
{
    /// <summary>
    /// Verifies that a line starts just past the last CR or LF before the position, and that the first line starts at
    /// zero.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The text holding the position.</param>
    /// <param name="position">The position.</param>
    /// <param name="expected">The expected start of its line.</param>
    [TestMethod]
    [DataRow("first line", "abc\ndef", 2, 0)]
    [DataRow("after a LF", "abc\ndef", 6, 4)]
    [DataRow("after a CR LF", "abc\r\ndef", 7, 5)]
    [DataRow("after a lone CR", "abc\rdef", 6, 4)]
    [DataRow("at the start of a line", "abc\ndef", 4, 4)]
    [DataRow("at a line ending", "abc\ndef", 3, 0)]
    public void LineStart_WhenThePositionIsOnALine_ShouldReturnTheStartOfThatLine(string testName, string source, int position, int expected)
    {
        Assert.AreEqual(expected, Utf8LineCursor.LineStart(Encoding.UTF8.GetBytes(source), position), testName);
    }

    /// <summary>
    /// Verifies that the first line of a source that starts with a byte-order mark starts past the mark, and that a later
    /// line is not affected by it.
    /// </summary>
    [TestMethod]
    public void LineStart_WhenTheSourceStartsWithAByteOrderMark_ShouldStartTheFirstLinePastIt()
    {
        byte[] source = [0xEF, 0xBB, 0xBF, .. "ab\ncd"u8];

        Assert.AreEqual(3, Utf8LineCursor.LineStart(source, 4), "first line");
        Assert.AreEqual(6, Utf8LineCursor.LineStart(source, 7), "second line");
    }
}
