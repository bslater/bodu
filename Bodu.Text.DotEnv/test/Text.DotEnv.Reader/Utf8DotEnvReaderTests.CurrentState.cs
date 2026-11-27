// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.CurrentState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv.Reader;

public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Verifies that a reader resumed from <see cref="Utf8DotEnvReader.CurrentState" /> on a later line keeps counting
    /// lines from the start of the whole input.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenResumedOnALaterLine_ShouldKeepLineNumbers()
    {
        byte[] input = "A=1\nB=2\nC="u8.ToArray();
        var first = new Utf8DotEnvReader(input, isFinalBlock: false, new DotEnvReaderState());
        while (first.Read())
        {
        }

        // Both whole entries are read; the third, which the block cuts short, is left for the next block.
        Assert.AreEqual(8, first.BytesConsumed);

        byte[] rest = [.. input[first.BytesConsumed..], .. "3\n"u8.ToArray()];
        var next = new Utf8DotEnvReader(rest, isFinalBlock: true, first.CurrentState);

        Assert.IsTrue(next.Read());
        Assert.AreEqual(DotEnvTokenType.PropertyName, next.TokenType);
        Assert.AreEqual("C", next.GetString());
        Assert.AreEqual(3, next.LineNumber);
    }

    /// <summary>
    /// Verifies that a <see cref="DotEnvFormatException" /> raised by a reader resumed part way through a line reports
    /// the line, the column and the offset in the whole input, the column counted from the start of the line in an
    /// earlier block.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenResumedPartWayThroughALine_ShouldReportTheErrorInTheWholeInput()
    {
        byte[] input = "A=1\n  1KEY=v\n"u8.ToArray();
        var first = new Utf8DotEnvReader(input.AsSpan(0, 5), isFinalBlock: false, new DotEnvReaderState());
        while (first.Read())
        {
        }

        // The indentation the block holds is consumed, so the next block begins part way through line 2.
        Assert.AreEqual(5, first.BytesConsumed);

        DotEnvReaderState state = first.CurrentState;
        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            var next = new Utf8DotEnvReader(input.AsSpan(5), isFinalBlock: true, state);
            while (next.Read())
            {
            }
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(3, ex.ColumnNumber);
        Assert.AreEqual(6, ex.Offset);
    }

    /// <summary>
    /// Verifies that a state captured after a property name carries its value, raw bytes and <c>export</c> prefix
    /// included, so a reader built from it over the rest of the input reports the value before it reads on.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenCapturedAfterAPropertyName_ShouldReportItsValue()
    {
        byte[] input = "export A=\"x\\ty\"\nB=2\n"u8.ToArray();
        var reader = new Utf8DotEnvReader(input, isFinalBlock: true, new DotEnvReaderState());

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("A", reader.GetString());

        var next = new Utf8DotEnvReader(input.AsSpan(reader.BytesConsumed), isFinalBlock: true, reader.CurrentState);

        Assert.IsTrue(next.Read());
        Assert.AreEqual(DotEnvTokenType.String, next.TokenType);
        Assert.AreEqual("x\ty", next.GetString());
        Assert.AreEqual("x\\ty", Encoding.UTF8.GetString(next.ValueSpan));
        Assert.IsTrue(next.CurrentIsExport);
        Assert.AreEqual(1, next.LineNumber);
        Assert.IsTrue(next.Read());
        Assert.AreEqual("B", next.GetString());
        Assert.IsFalse(next.CurrentIsExport);
        Assert.AreEqual(2, next.LineNumber);
    }

    /// <summary>
    /// Verifies that a state captured once the document object has opened carries that, so the next block's reader does
    /// not open it again.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenCapturedInsideTheDocument_ShouldNotReportItsStartAgain()
    {
        var first = new Utf8DotEnvReader("A"u8, isFinalBlock: false, new DotEnvReaderState());
        while (first.Read())
        {
        }

        var next = new Utf8DotEnvReader("A=1\n"u8, isFinalBlock: true, first.CurrentState);

        Assert.AreEqual(1, next.CurrentDepth);
        Assert.IsTrue(next.Read());
        Assert.AreEqual(DotEnvTokenType.PropertyName, next.TokenType);
    }
}
