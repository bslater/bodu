// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.StreamsAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Text.Formats;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.DecodeAsync(Stream, CancellationToken)" /> propagates cancellation when
    /// the supplied token transitions to cancelled before the read completes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DecodeAsync_WhenCancellationTokenSignalled_ShouldThrowOperationCanceledException()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        using MemoryStream stream = new(CanonicalSpamBytes);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            _ = await Bencode.DecodeAsync(stream, cts.Token);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.DecodeAsync(Stream, CancellationToken)" /> handles a non-seekable source
    /// by buffering through <see cref="Stream.CopyToAsync(Stream, CancellationToken)" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DecodeAsync_WhenNonSeekableSource_ShouldReturnDecodedValue()
    {
        using NonSeekableStream stream = new(CanonicalIntegerBytes);

        BencodedValue value = await Bencode.DecodeAsync(stream);

        Assert.AreEqual(42, ((BencodedInteger)value).Value);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.DecodeAsync(Stream, CancellationToken)" /> rejects a non-readable stream
    /// without waiting for I/O.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DecodeAsync_WhenNotReadable_ShouldThrowArgumentException()
    {
        using MemoryStream stream = new();
        stream.Close();

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await Bencode.DecodeAsync(stream);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.DecodeAsync(Stream, CancellationToken)" /> reads asynchronously and
    /// decodes correctly from a seekable <see cref="MemoryStream" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DecodeAsync_WhenSeekableMemoryStream_ShouldReturnDecodedValue()
    {
        using MemoryStream stream = new(CanonicalSpamBytes);

        BencodedValue value = await Bencode.DecodeAsync(stream);

        Assert.AreEqual("spam", ((BencodedString)value).GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.EncodeAsync(BencodedValue, Stream, CancellationToken)" /> rejects a
    /// non-writable destination without waiting for I/O.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EncodeAsync_WhenNotWritable_ShouldThrowArgumentException()
    {
        using MemoryStream stream = new(new byte[8], writable: false);

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await Bencode.EncodeAsync(new BencodedInteger(42), stream);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.EncodeAsync(BencodedValue, Stream, CancellationToken)" /> writes the
    /// canonical bytes into a <see cref="MemoryStream" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EncodeAsync_WhenWritingToMemoryStream_ShouldWriteCanonicalBytes()
    {
        using MemoryStream stream = new();

        await Bencode.EncodeAsync(new BencodedInteger(42), stream);

        CollectionAssert.AreEqual(CanonicalIntegerBytes, stream.ToArray());
    }

}
