// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that reading the full payload through the stream accumulates the same digest as hashing the
    /// payload directly.
    /// </summary>
    [TestMethod]
    public void Read_WhenPayloadFullyRead_ShouldAccumulateSameDigestAsDirectHash()
    {
        byte[] payload = CreatePayload(1024);
        using MemoryStream inner = new(payload);
        using HashingStream stream = new(inner, new Fnv1a32());

        byte[] buffer = new byte[payload.Length];
        int totalRead = 0;
        int bytesRead;
        while ((bytesRead = stream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
            totalRead += bytesRead;

        Assert.AreEqual(payload.Length, totalRead);
        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that partial reads against a chunk-limited inner stream append only the bytes actually read, so
    /// the final digest still matches the direct hash.
    /// </summary>
    [TestMethod]
    public void Read_WhenInnerStreamReturnsPartialChunks_ShouldHashOnlyBytesActuallyRead()
    {
        byte[] payload = CreatePayload(257);
        using FixedChunkStream inner = new(payload, 7);
        using HashingStream stream = new(inner, new Fnv1a32());

        byte[] buffer = new byte[64];
        while (stream.Read(buffer, 0, buffer.Length) > 0)
        {
        }

        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that reading from a non-seekable inner stream works and produces the direct-hash digest.
    /// </summary>
    [TestMethod]
    public void Read_WhenInnerStreamIsNonSeekable_ShouldAccumulateSameDigestAsDirectHash()
    {
        byte[] payload = CreatePayload(512);
        using NonSeekableStream inner = new(payload);
        using HashingStream stream = new(inner, new Fnv1a32());

        stream.CopyTo(Stream.Null);

        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.ReadByte" /> returns each byte and contributes it to the digest,
    /// then returns <c>-1</c> at end of stream.
    /// </summary>
    [TestMethod]
    public void ReadByte_WhenPayloadConsumedByteWise_ShouldHashEveryByteAndSignalEnd()
    {
        byte[] payload = new byte[] { 0x10, 0x20, 0x30 };
        using MemoryStream inner = new(payload);
        using HashingStream stream = new(inner, new Fnv1a32());

        Assert.AreEqual(0x10, stream.ReadByte());
        Assert.AreEqual(0x20, stream.ReadByte());
        Assert.AreEqual(0x30, stream.ReadByte());
        Assert.AreEqual(-1, stream.ReadByte());

        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that the array-based <see cref="HashingStream.Read(byte[], int, int)" /> overload rejects an
    /// invalid offset/count segment.
    /// </summary>
    [TestMethod]
    public void Read_WhenOffsetAndCountExceedBuffer_ShouldThrowArgumentException()
    {
        using MemoryStream inner = new([0x01, 0x02]);
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = stream.Read(new byte[4], 2, 3);
        });
    }

    /// <summary>
    /// Verifies that the array-based <see cref="HashingStream.Read(byte[], int, int)" /> overload throws
    /// <see cref="ArgumentNullException" /> for a null buffer.
    /// </summary>
    [TestMethod]
    public void Read_WhenBufferIsNull_ShouldThrowArgumentNullException()
    {
        using MemoryStream inner = new([0x01]);
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = stream.Read(null!, 0, 1);
        });
    }
}
