// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReaderWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for the <see cref="BencodeReader" /> and <see cref="BencodeWriter" /> types — the deserialization
/// and serialization halves of the read/write pair that the static <see cref="Bencode" /> facade delegates to.
/// </summary>
[TestClass]
public sealed class BencodeReaderWriterTests
{
    /// <summary>
    /// Canonical encoded form of the list <c>[42, "spam"]</c> (<c>li42e4:spame</c>).
    /// </summary>
    private static readonly byte[] CanonicalListBytes = System.Text.Encoding.ASCII.GetBytes("li42e4:spame");

    /// <summary>
    /// Builds the <c>[42, "spam"]</c> list value that <see cref="CanonicalListBytes" /> encodes.
    /// </summary>
    /// <returns>The decoded list value.</returns>
    private static BencodedList SampleList() =>
        new(new BencodedValue[] { new BencodedInteger(42), BencodedString.FromUtf8("spam") });

    /// <summary>
    /// Asserts that <paramref name="value" /> is the <c>[42, "spam"]</c> list. Used in place of value equality because
    /// <see cref="BencodedList" /> compares by reference.
    /// </summary>
    /// <param name="value">The decoded value to inspect.</param>
    private static void AssertSampleList(BencodedValue value)
    {
        var list = (BencodedList)value;

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(new BencodedInteger(42), list[0]);
        Assert.AreEqual(BencodedString.FromUtf8("spam"), list[1]);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReader.Read(ReadOnlySpan{byte})" /> decodes a complete document from a span.
    /// </summary>
    [TestMethod]
    public void Reader_WhenReadSpan_ShouldDecode()
    {
        BencodedValue document = new BencodeReader().Read(CanonicalListBytes);

        AssertSampleList(document);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReader.Read(byte[])" /> decodes a complete document from a byte array.
    /// </summary>
    [TestMethod]
    public void Reader_WhenReadByteArray_ShouldDecode()
    {
        BencodedValue document = new BencodeReader().Read(CanonicalListBytes);

        AssertSampleList(document);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReader.Read(Stream)" /> decodes a complete document from a stream.
    /// </summary>
    [TestMethod]
    public void Reader_WhenReadStream_ShouldDecode()
    {
        using var stream = new MemoryStream(CanonicalListBytes);
        BencodedValue document = new BencodeReader().Read(stream);

        AssertSampleList(document);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReader.ReadAsync(Stream, CancellationToken)" /> decodes a document from a stream.
    /// </summary>
    [TestMethod]
    public async Task Reader_WhenReadAsyncStream_ShouldDecode()
    {
        using var stream = new MemoryStream(CanonicalListBytes);
        BencodedValue document = await new BencodeReader().ReadAsync(stream);

        AssertSampleList(document);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReader.TryRead(ReadOnlySpan{byte}, out BencodedValue?, out int)" /> reports the
    /// outcome and the number of bytes consumed for both valid and malformed input.
    /// </summary>
    [TestMethod]
    public void Reader_WhenTryReadValidAndInvalid_ShouldReflectOutcome()
    {
        var reader = new BencodeReader();

        Assert.IsTrue(reader.TryRead(CanonicalListBytes, out BencodedValue? document, out int consumed));
        AssertSampleList(document);
        Assert.AreEqual(CanonicalListBytes.Length, consumed);

        Assert.IsFalse(reader.TryRead(System.Text.Encoding.ASCII.GetBytes("i42"), out BencodedValue? failed, out int failedConsumed));
        Assert.IsNull(failed);
        Assert.AreEqual(0, failedConsumed);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReader.Read(byte[])" /> throws when the byte array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Reader_WhenReadNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodeReader().Read((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReader.Read(Stream)" /> throws when the stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Reader_WhenReadNullStream_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodeReader().Read((Stream)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeParseOptions.MaxDepth" /> flows through the reader and rejects input that nests
    /// more deeply than the configured limit.
    /// </summary>
    [TestMethod]
    public void Reader_WhenNestingExceedsMaxDepth_ShouldThrowExactly()
    {
        byte[] nested = System.Text.Encoding.ASCII.GetBytes("llleee");
        var options = new BencodeParseOptions { MaxDepth = 2 };

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = new BencodeReader().Read(nested, options);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeParseOptions.RequireCompleteDocument" /> flows through the reader so that
    /// <c>TryRead</c> rejects trailing bytes when set but accepts them otherwise.
    /// </summary>
    [TestMethod]
    public void Reader_WhenTryReadWithRequireCompleteDocument_ShouldHonourTrailingBytes()
    {
        var reader = new BencodeReader();
        byte[] trailing = System.Text.Encoding.ASCII.GetBytes("i42ei7e");

        Assert.IsTrue(reader.TryRead(trailing, BencodeParseOptions.Default, out BencodedValue? lenient, out int consumed));
        Assert.AreEqual(new BencodedInteger(42), lenient);
        Assert.AreEqual(4, consumed);

        var strict = new BencodeParseOptions { RequireCompleteDocument = true };
        Assert.IsFalse(reader.TryRead(trailing, strict, out BencodedValue? complete, out _));
        Assert.IsNull(complete);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeWriter.Write(BencodedValue)" /> encodes a value into its canonical bytes.
    /// </summary>
    [TestMethod]
    public void Writer_WhenWrite_ShouldEncode()
    {
        byte[] encoded = new BencodeWriter().Write(SampleList());

        CollectionAssert.AreEqual(CanonicalListBytes, encoded);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeWriter.TryWrite(BencodedValue, Span{byte}, out int)" /> encodes into a
    /// sufficiently large buffer and reports failure for a buffer that is too small.
    /// </summary>
    [TestMethod]
    public void Writer_WhenTryWrite_ShouldReflectBufferCapacity()
    {
        var writer = new BencodeWriter();
        BencodedList value = SampleList();

        var destination = new byte[CanonicalListBytes.Length];
        Assert.IsTrue(writer.TryWrite(value, destination, out int written));
        Assert.AreEqual(CanonicalListBytes.Length, written);
        CollectionAssert.AreEqual(CanonicalListBytes, destination);

        Assert.IsFalse(writer.TryWrite(value, new byte[CanonicalListBytes.Length - 1], out int shortWritten));
        Assert.AreEqual(0, shortWritten);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeWriter.GetWrittenLength(BencodedValue)" /> returns the exact encoded length.
    /// </summary>
    [TestMethod]
    public void Writer_WhenGetWrittenLength_ShouldReturnExactLength()
    {
        Assert.AreEqual(CanonicalListBytes.Length, new BencodeWriter().GetWrittenLength(SampleList()));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeWriter.Write(BencodedValue, Stream)" /> encodes a value to a stream.
    /// </summary>
    [TestMethod]
    public void Writer_WhenWriteStream_ShouldEncode()
    {
        using var stream = new MemoryStream();
        new BencodeWriter().Write(SampleList(), stream);

        CollectionAssert.AreEqual(CanonicalListBytes, stream.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeWriter.WriteAsync(BencodedValue, Stream, CancellationToken)" /> encodes a value
    /// to a stream.
    /// </summary>
    [TestMethod]
    public async Task Writer_WhenWriteAsyncStream_ShouldEncode()
    {
        using var stream = new MemoryStream();
        await new BencodeWriter().WriteAsync(SampleList(), stream);

        CollectionAssert.AreEqual(CanonicalListBytes, stream.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeWriter.Write(BencodedValue)" /> throws when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Writer_WhenWriteNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodeWriter().Write(null!);
        });
    }

    /// <summary>
    /// Verifies that the reader and the static <see cref="Bencode" /> facade decode identical input to equal results,
    /// confirming the facade delegates faithfully.
    /// </summary>
    [TestMethod]
    public void ReaderAndFacade_WhenSameInput_ShouldYieldEqualResult()
    {
        BencodedValue viaReader = new BencodeReader().Read(CanonicalListBytes);
        BencodedValue viaFacade = Bencode.Parse(CanonicalListBytes);

        var writer = new BencodeWriter();
        CollectionAssert.AreEqual(writer.Write(viaReader), writer.Write(viaFacade));
    }

    /// <summary>
    /// Verifies that encoding a value and decoding it again through the read/write pair yields the original value, and
    /// that re-encoding produces identical bytes.
    /// </summary>
    [TestMethod]
    public void ReaderWriter_WhenRoundTripped_ShouldYieldEqualBytes()
    {
        var reader = new BencodeReader();
        var writer = new BencodeWriter();

        byte[] once = writer.Write(reader.Read(CanonicalListBytes));
        byte[] twice = writer.Write(reader.Read(once));

        CollectionAssert.AreEqual(once, twice);
        CollectionAssert.AreEqual(CanonicalListBytes, once);
    }
}
