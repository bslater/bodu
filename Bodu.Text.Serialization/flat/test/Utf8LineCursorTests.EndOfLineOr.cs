// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.EndOfLineOr.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8LineCursorTests
{
    /// <summary>
    /// Verifies that a scan for a stop byte ends at whichever of the stop byte, a CR or a LF comes first from the
    /// position, and at the end of the source when none does.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The text to scan.</param>
    /// <param name="position">The position from which to scan.</param>
    /// <param name="expected">The expected position.</param>
    [TestMethod]
    [DataRow("stop byte first", "ab=c\nd", 0, 2)]
    [DataRow("LF first", "ab\nc=d", 0, 2)]
    [DataRow("CR first", "ab\rc=d", 0, 2)]
    [DataRow("stop byte at the position", "=ab", 0, 0)]
    [DataRow("stop byte before the position", "a=b=c", 2, 3)]
    [DataRow("none remains", "abc", 0, 3)]
    [DataRow("long run before the stop byte", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa=", 0, 70)]
    public void EndOfLineOr_WhenTheSourceHoldsTheStopByteOrALineEnding_ShouldReturnTheFirst(string testName, string source, int position, int expected)
    {
        Assert.AreEqual(expected, Utf8LineCursor.EndOfLineOr(Encoding.UTF8.GetBytes(source), position, (byte)'='), testName);
    }

    /// <summary>
    /// Verifies that a scan from the end of the source, or past it, returns its starting position, as a scan a byte at
    /// a time did.
    /// </summary>
    /// <param name="position">The position from which to scan.</param>
    [TestMethod]
    [DataRow(3)]
    [DataRow(4)]
    public void EndOfLineOr_WhenThePositionIsAtOrPastTheEnd_ShouldReturnThePosition(int position)
    {
        Assert.AreEqual(position, Utf8LineCursor.EndOfLineOr("abc"u8, position, (byte)'='));
    }
}
