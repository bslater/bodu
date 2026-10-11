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
}
