// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.EndOfLine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8LineCursorTests
{
    /// <summary>
    /// Verifies that the end of a line is the position of its CR, LF or CRLF terminator, which is not consumed.
    /// </summary>
    /// <param name="source">The text to scan.</param>
    [TestMethod]
    [DataRow("abc\r\ndef")]
    [DataRow("abc\ndef")]
    [DataRow("abc\rdef")]
    public void EndOfLine_WhenLineHasTerminator_ShouldReturnTerminatorPosition(string source)
    {
        Assert.AreEqual(3, Utf8LineCursor.EndOfLine(Encoding.UTF8.GetBytes(source), 0));
    }

    /// <summary>
    /// Verifies that the end of a line with no terminator is the end of the source.
    /// </summary>
    [TestMethod]
    public void EndOfLine_WhenNoTerminatorRemains_ShouldReturnSourceLength()
    {
        Assert.AreEqual(3, Utf8LineCursor.EndOfLine("abc"u8, 0));
    }

    /// <summary>
    /// Verifies that the end of a line is found past a run longer than a vector of bytes, from a position inside the
    /// source.
    /// </summary>
    [TestMethod]
    public void EndOfLine_WhenTheLineIsLongerThanAVector_ShouldReturnTerminatorPosition()
    {
        byte[] source = Encoding.UTF8.GetBytes("x\n" + new string('a', 100) + "\r\nb");

        Assert.AreEqual(102, Utf8LineCursor.EndOfLine(source, 2));
    }

    /// <summary>
    /// Verifies that a scan from the end of the source, or past it, returns its starting position, as a scan a byte at
    /// a time did.
    /// </summary>
    /// <param name="position">The position from which to scan.</param>
    [TestMethod]
    [DataRow(3)]
    [DataRow(4)]
    public void EndOfLine_WhenThePositionIsAtOrPastTheEnd_ShouldReturnThePosition(int position)
    {
        Assert.AreEqual(position, Utf8LineCursor.EndOfLine("abc"u8, position));
    }
}
