// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.WriteAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that asynchronously writing the payload forwards identical bytes to the inner stream and
    /// accumulates the same digest as hashing the payload directly.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WriteAsync_WhenPayloadWritten_ShouldForwardBytesAndAccumulateSameDigest()
    {
        var payload = CreatePayload(2048);
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        await stream.WriteAsync(payload.AsMemory());
        await stream.FlushAsync(CancellationToken.None);

        CollectionAssert.AreEqual(payload, inner.ToArray());
        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that the array-based
    /// <see cref="HashingStream.WriteAsync(byte[], int, int, CancellationToken)" /> overload forwards the segment
    /// and contributes it to the digest.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WriteAsync_ForArrayOverload_ShouldForwardSegmentAndHashIt()
    {
        var payload = CreatePayload(100);
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        await stream.WriteAsync(payload, 0, payload.Length, CancellationToken.None);

        CollectionAssert.AreEqual(payload, inner.ToArray());
        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }
}
