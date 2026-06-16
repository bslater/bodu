// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.ReadAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that asynchronously reading the full payload through the stream accumulates the same digest as
    /// hashing the payload directly.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_WhenPayloadFullyRead_ShouldAccumulateSameDigestAsDirectHash()
    {
        byte[] payload = CreatePayload(2048);
        using MemoryStream inner = new(payload);
        using HashingStream stream = new(inner, new Fnv1a32());

        await stream.CopyToAsync(Stream.Null);

        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that the array-based
    /// <see cref="HashingStream.ReadAsync(byte[], int, int, CancellationToken)" /> overload reads bytes and
    /// contributes them to the digest.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_ForArrayOverload_ShouldHashBytesActuallyRead()
    {
        byte[] payload = CreatePayload(100);
        using MemoryStream inner = new(payload);
        using HashingStream stream = new(inner, new Fnv1a32());

        byte[] buffer = new byte[payload.Length];
        int totalRead = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, CancellationToken.None)) > 0)
            totalRead += bytesRead;

        Assert.AreEqual(payload.Length, totalRead);
        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }
}
