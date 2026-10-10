// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.Consolidation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>Direct regression tests for the shared byte-oriented line cursor used by this reader.</summary>
[TestClass]
public class Utf8LineCursorTests
{
    [TestMethod]
    [DataRow("abc\r\ndef", 5)]
    [DataRow("abc\ndef", 4)]
    [DataRow("abc\rdef", 4)]
    [DataRow("abc", 3)]
    public void EndOfLine_WhenMixedTerminator_ShouldNotConsumeTerminator(string source, int expectedAfterTerminator)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        int end = Utf8LineCursor.EndOfLine(bytes, 0);
        Assert.AreEqual(3, end);
        int position = end;
        int consumed = Utf8LineCursor.ConsumeLineEnding(bytes, ref position);
        Assert.AreEqual(expectedAfterTerminator - end, position - end);
        Assert.AreEqual(source.Length == 3 ? 0 : 1, consumed);
    }

    [TestMethod]
    public void ConsumeLineEnding_WhenTerminatorIsNotAtCursor_ShouldLeavePositionUnchanged()
    {
        byte[] bytes = "abc\n"u8.ToArray();
        int position = 1;
        Assert.AreEqual(0, Utf8LineCursor.ConsumeLineEnding(bytes, ref position));
        Assert.AreEqual(1, position);
    }
}
