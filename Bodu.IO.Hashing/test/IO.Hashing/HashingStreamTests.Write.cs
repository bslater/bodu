// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that writing the payload through the stream forwards identical bytes to the inner stream and
    /// accumulates the same digest as hashing the payload directly.
    /// </summary>
    [TestMethod]
    public void Write_WhenPayloadWritten_ShouldForwardBytesAndAccumulateSameDigest()
    {
        var payload = CreatePayload(1024);
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        stream.Write(payload, 0, payload.Length);

        CollectionAssert.AreEqual(payload, inner.ToArray());
        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that writing in multiple segments produces the same digest as a single direct hash over the
    /// concatenated payload.
    /// </summary>
    [TestMethod]
    public void Write_WhenPayloadWrittenInSegments_ShouldMatchSingleShotDigest()
    {
        var payload = CreatePayload(300);
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        stream.Write(payload.AsSpan(0, 100));
        stream.Write(payload.AsSpan(100, 150));
        stream.Write(payload.AsSpan(250, 50));

        CollectionAssert.AreEqual(payload, inner.ToArray());
        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.WriteByte" /> forwards the byte and contributes it to the digest.
    /// </summary>
    [TestMethod]
    public void WriteByte_WhenBytesWrittenIndividually_ShouldForwardAndHashEveryByte()
    {
        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        foreach (var value in payload)
            stream.WriteByte(value);

        CollectionAssert.AreEqual(payload, inner.ToArray());
        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that the array-based <see cref="HashingStream.Write(byte[], int, int)" /> overload rejects an
    /// invalid offset/count segment.
    /// </summary>
    [TestMethod]
    public void Write_WhenOffsetAndCountExceedBuffer_ShouldThrowArgumentException()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            stream.Write(new byte[4], 3, 2);
        });
    }

    /// <summary>
    /// Verifies that the array-based <see cref="HashingStream.Write(byte[], int, int)" /> overload throws
    /// <see cref="ArgumentNullException" /> for a null buffer.
    /// </summary>
    [TestMethod]
    public void Write_WhenBufferIsNull_ShouldThrowArgumentNullException()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            stream.Write(null!, 0, 1);
        });
    }
}
