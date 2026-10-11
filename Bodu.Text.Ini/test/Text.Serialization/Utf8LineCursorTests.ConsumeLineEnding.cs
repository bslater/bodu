// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.ConsumeLineEnding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8LineCursorTests
{
    /// <summary>
    /// Verifies that a CR, LF or CRLF at the cursor is consumed as one line ending.
    /// </summary>
    /// <param name="source">The text whose terminator starts at position 3.</param>
    /// <param name="expectedPosition">The position just past the terminator.</param>
    [TestMethod]
    [DataRow("abc\r\ndef", 5)]
    [DataRow("abc\ndef", 4)]
    [DataRow("abc\rdef", 4)]
    public void ConsumeLineEnding_WhenTerminatorIsAtCursor_ShouldConsumeIt(string source, int expectedPosition)
    {
        int position = 3;

        int consumed = Utf8LineCursor.ConsumeLineEnding(Encoding.UTF8.GetBytes(source), ref position);

        Assert.AreEqual(1, consumed);
        Assert.AreEqual(expectedPosition, position);
    }

    /// <summary>
    /// Verifies that nothing is consumed at the end of the source.
    /// </summary>
    [TestMethod]
    public void ConsumeLineEnding_WhenCursorIsAtEnd_ShouldConsumeNothing()
    {
        int position = 3;

        Assert.AreEqual(0, Utf8LineCursor.ConsumeLineEnding("abc"u8, ref position));
        Assert.AreEqual(3, position);
    }

    /// <summary>
    /// Verifies that the cursor does not move when it is not on a terminator.
    /// </summary>
    [TestMethod]
    public void ConsumeLineEnding_WhenTerminatorIsNotAtCursor_ShouldLeavePositionUnchanged()
    {
        int position = 1;

        Assert.AreEqual(0, Utf8LineCursor.ConsumeLineEnding("abc\n"u8, ref position));
        Assert.AreEqual(1, position);
    }
}
