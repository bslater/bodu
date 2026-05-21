// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.Streams.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(Stream)" /> handles non-seekable streams by buffering reads
    /// through <see cref="Stream.CopyTo(Stream)" />. Uses <see cref="NonSeekableStream" /> to simulate a
    /// network-style source.
    /// </summary>
    [TestMethod]
    public void DecodeStream_WhenNonSeekableSource_ShouldReturnDecodedValue()
    {
        using NonSeekableStream stream = new(CanonicalSpamBytes);

        BencodedValue value = Bencode.Decode(stream);

        Assert.AreEqual("spam", ((BencodedString)value).GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(Stream)" /> rejects a non-readable stream with
    /// <see cref="ArgumentException" /> using the resx-backed message.
    /// </summary>
    [TestMethod]
    public void DecodeStream_WhenNotReadable_ShouldThrowArgumentException()
    {
        using MemoryStream stream = new();
        stream.Close();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Bencode.Decode(stream);
        });

        Assert.AreEqual("source", ex.ParamName);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.Arg_Invalid_StreamNotReadable);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(Stream)" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void DecodeStream_WhenNullSource_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.Decode((Stream)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(Stream)" /> decodes a value from a seekable
    /// <see cref="MemoryStream" />.
    /// </summary>
    [TestMethod]
    public void DecodeStream_WhenSeekableMemoryStream_ShouldReturnDecodedValue()
    {
        using MemoryStream stream = new(CanonicalIntegerBytes);

        BencodedValue value = Bencode.Decode(stream);

        Assert.AreEqual(42, ((BencodedInteger)value).Value);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(Stream)" /> handles a source that returns small fixed-size chunks
    /// (e.g. a slow network read), using <see cref="FixedChunkStream" /> with a 2-byte chunk size.
    /// </summary>
    [TestMethod]
    public void DecodeStream_WhenSourceReturnsSmallChunks_ShouldReturnDecodedValue()
    {
        using FixedChunkStream stream = new(Bytes("l4:spami42ee"), chunkSize: 2);

        BencodedValue value = Bencode.Decode(stream);

        var list = (BencodedList)value;
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("spam", ((BencodedString)list[0]).GetUtf8String());
        Assert.AreEqual(42, ((BencodedInteger)list[1]).Value);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue, Stream)" /> rejects a non-writable stream.
    /// </summary>
    [TestMethod]
    public void EncodeStream_WhenNotWritable_ShouldThrowArgumentException()
    {
        // A read-only MemoryStream is non-writable.
        using MemoryStream stream = new(new byte[8], writable: false);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Bencode.Encode(new BencodedInteger(42), stream);
        });

        Assert.AreEqual("destination", ex.ParamName);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.Arg_Invalid_StreamNotWritable);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue, Stream)" /> rejects a <see langword="null" />
    /// destination.
    /// </summary>
    [TestMethod]
    public void EncodeStream_WhenNullDestination_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            Bencode.Encode(new BencodedInteger(42), (Stream)null!);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue, Stream)" /> rejects a <see langword="null" />
    /// value.
    /// </summary>
    [TestMethod]
    public void EncodeStream_WhenNullValue_ShouldThrowArgumentNullException()
    {
        using MemoryStream stream = new();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            Bencode.Encode(null!, stream);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue, Stream)" /> writes the canonical bytes into a
    /// <see cref="MemoryStream" />.
    /// </summary>
    [TestMethod]
    public void EncodeStream_WhenWritingToMemoryStream_ShouldWriteCanonicalBytes()
    {
        using MemoryStream stream = new();

        Bencode.Encode(new BencodedInteger(42), stream);

        CollectionAssert.AreEqual(CanonicalIntegerBytes, stream.ToArray());
    }

}
