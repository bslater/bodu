// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.CurrentState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Delimited.Reader;

public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that a reader resumed from <see cref="Utf8DelimitedReader.CurrentState" /> on a later line keeps
    /// counting lines and offsets from the start of the whole input, so its records report the positions a single read
    /// gives.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenResumedOnALaterLine_ShouldKeepLineNumbersAndOffsets()
    {
        byte[] input = "a,b\nc,d\ne,"u8.ToArray();
        var first = new Utf8DelimitedReader(input, isFinalBlock: false, new DelimitedReaderState(new DelimitedReaderOptions { NoHeader = true }));
        while (first.Read())
        {
        }

        // Both whole records are read; the third, which the block cuts short, is left for the next block.
        Assert.AreEqual(8, first.BytesConsumed);

        byte[] rest = [.. input[first.BytesConsumed..], .. "f\n"u8.ToArray()];
        var next = new Utf8DelimitedReader(rest, isFinalBlock: true, first.CurrentState);

        Assert.IsTrue(next.Read());
        Assert.AreEqual(DelimitedTokenType.StartArray, next.TokenType);
        Assert.AreEqual(3, next.RecordLine);
        Assert.AreEqual(8, next.RecordOffset);
        Assert.IsTrue(next.Read());
        Assert.AreEqual("e", next.GetString());
    }

    /// <summary>
    /// Verifies that a <see cref="DelimitedFormatException" /> raised by a resumed reader reports the line and the
    /// offset in the whole input, not in its block.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenResumedBeforeAMalformedRecord_ShouldReportTheErrorInTheWholeInput()
    {
        byte[] input = "a,b\nc,\"d\"x\n"u8.ToArray();
        var first = new Utf8DelimitedReader(input.AsSpan(0, 5), isFinalBlock: false, new DelimitedReaderState(new DelimitedReaderOptions { NoHeader = true }));
        while (first.Read())
        {
        }

        DelimitedReaderState state = first.CurrentState;
        int consumed = first.BytesConsumed;

        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            var next = new Utf8DelimitedReader(input.AsSpan(consumed), isFinalBlock: true, state);
            while (next.Read())
            {
            }
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(9, ex.Offset);
    }

    /// <summary>
    /// Verifies that a state captured between the tokens of a record carries the record's fields left to report, raw
    /// bytes included, so a reader built from it over the rest of the input reports them before it reads on.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenCapturedBetweenTheTokensOfARecord_ShouldReportTheRestOfTheRecord()
    {
        byte[] input = "name,city\nAda,\"Lon\"\"don\"\nBob,Paris\n"u8.ToArray();
        var reader = new Utf8DelimitedReader(input, isFinalBlock: true, new DelimitedReaderState());

        // StartArray, StartObject, then the name of the first field.
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("name", reader.GetString());

        DelimitedReaderState state = reader.CurrentState;
        var next = new Utf8DelimitedReader(input.AsSpan(reader.BytesConsumed), isFinalBlock: true, state);

        Assert.IsTrue(next.Read());
        Assert.AreEqual("Ada", next.GetString());
        Assert.IsTrue(next.Read());
        Assert.AreEqual("city", next.GetString());
        Assert.IsTrue(next.Read());
        Assert.AreEqual("Lon\"don", next.GetString());
        Assert.AreEqual("Lon\"\"don", Encoding.UTF8.GetString(next.ValueSpan));
        Assert.IsTrue(next.Read());
        Assert.AreEqual(DelimitedTokenType.EndObject, next.TokenType);
        Assert.IsTrue(next.Read());
        Assert.AreEqual(DelimitedTokenType.StartObject, next.TokenType);
        Assert.IsTrue(next.Read());
        Assert.IsTrue(next.Read());
        Assert.AreEqual("Bob", next.GetString());
        Assert.AreEqual("Bob", Encoding.UTF8.GetString(next.ValueSpan));
    }

    /// <summary>
    /// Verifies that the state a reader captures once the header row is read carries the header names, so the next
    /// block's reader names its fields by them.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenCapturedAfterTheHeaderRow_ShouldCarryTheHeaderNames()
    {
        var first = new Utf8DelimitedReader("id,score\n1,"u8, isFinalBlock: false, new DelimitedReaderState());
        while (first.Read())
        {
        }

        var next = new Utf8DelimitedReader("1,9\n"u8, isFinalBlock: true, first.CurrentState);

        CollectionAssert.AreEqual(new[] { "id", "score" }, next.Headers.ToArray());
        Assert.IsTrue(next.Read());
        Assert.AreEqual(DelimitedTokenType.StartObject, next.TokenType);
        Assert.IsTrue(next.Read());
        Assert.AreEqual("id", next.GetString());
    }
}
