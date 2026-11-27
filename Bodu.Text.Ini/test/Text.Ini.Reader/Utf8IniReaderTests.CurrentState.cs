// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReaderTests.CurrentState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

public partial class Utf8IniReaderTests
{
    /// <summary>
    /// Verifies that a reader resumed from <see cref="Utf8IniReader.CurrentState" /> on a later line keeps counting
    /// lines and offsets from the start of the whole input.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenResumedOnALaterLine_ShouldKeepLineNumbersAndOffsets()
    {
        byte[] input = "[s]\nk=v\nj="u8.ToArray();
        var first = new Utf8IniReader(input, isFinalBlock: false, new IniReaderState());
        while (first.Read())
        {
        }

        // The header and the whole entry are read; the third line, which the block cuts short, is left for the next.
        Assert.AreEqual(8, first.BytesConsumed);

        byte[] rest = [.. input[first.BytesConsumed..], .. "w\n"u8.ToArray()];
        var next = new Utf8IniReader(rest, isFinalBlock: true, first.CurrentState);

        Assert.IsTrue(next.Read());
        Assert.AreEqual(IniTokenType.PropertyName, next.TokenType);
        Assert.AreEqual("j", next.GetString());
        Assert.AreEqual(3, next.LineNumber);
        Assert.AreEqual(8, next.TokenOffset);
        Assert.IsTrue(next.Read());
        Assert.AreEqual("w", next.GetString());
        Assert.AreEqual(10, next.TokenOffset);
    }

    /// <summary>
    /// Verifies that an <see cref="IniFormatException" /> raised by a resumed reader reports the line and the offset in
    /// the whole input, not in its block.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenResumedBeforeAMalformedLine_ShouldReportTheErrorInTheWholeInput()
    {
        byte[] input = "k=v\n[foo] bar\n"u8.ToArray();
        var first = new Utf8IniReader(input.AsSpan(0, 6), isFinalBlock: false, new IniReaderState());
        while (first.Read())
        {
        }

        IniReaderState state = first.CurrentState;
        int consumed = first.BytesConsumed;

        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            var next = new Utf8IniReader(input.AsSpan(consumed), isFinalBlock: true, state);
            while (next.Read())
            {
            }
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(10, ex.Offset);
    }

    /// <summary>
    /// Verifies that a state captured after a key carries its value, so a reader built from it over the rest of the
    /// input reports the value, at its line and offset, before it reads on.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenCapturedAfterAKey_ShouldReportItsValue()
    {
        byte[] input = "a = one\nb=two\n"u8.ToArray();
        var reader = new Utf8IniReader(input, isFinalBlock: true, new IniReaderState());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("a", reader.GetString());

        var next = new Utf8IniReader(input.AsSpan(reader.BytesConsumed), isFinalBlock: true, reader.CurrentState);

        Assert.IsTrue(next.Read());
        Assert.AreEqual(IniTokenType.String, next.TokenType);
        Assert.AreEqual("one", next.GetString());
        Assert.AreEqual(1, next.LineNumber);
        Assert.AreEqual(4, next.TokenOffset);
        Assert.IsTrue(next.Read());
        Assert.AreEqual("b", next.GetString());
        Assert.AreEqual(2, next.LineNumber);
        Assert.AreEqual(8, next.TokenOffset);
    }
}
