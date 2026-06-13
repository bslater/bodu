// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.GetCurrentHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that <see cref="HashingStream.GetCurrentHash" /> does not finalize the ongoing computation, so
    /// further writes continue accumulating into the same digest.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenCalledMidTransfer_ShouldNotFinalizeComputation()
    {
        var payload = CreatePayload(200);
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        stream.Write(payload.AsSpan(0, 100));
        _ = stream.GetCurrentHash();
        stream.Write(payload.AsSpan(100, 100));

        CollectionAssert.AreEqual(ComputeExpectedHash(payload), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.GetHashAndReset" /> returns the accumulated digest and resets the
    /// algorithm so subsequent transfers hash independently.
    /// </summary>
    [TestMethod]
    public void GetHashAndReset_WhenCalledBetweenTransfers_ShouldResetAccumulation()
    {
        var first = CreatePayload(64);
        var second = new byte[] { 0xFE, 0xDC, 0xBA };
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        stream.Write(first);
        CollectionAssert.AreEqual(ComputeExpectedHash(first), stream.GetHashAndReset());

        stream.Write(second);
        CollectionAssert.AreEqual(ComputeExpectedHash(second), stream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.GetCurrentHash" /> before any transfer reports the algorithm's
    /// initial-state digest.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenNoBytesTransferred_ShouldMatchInitialStateDigest()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        CollectionAssert.AreEqual(new Fnv1a32().GetCurrentHash(), stream.GetCurrentHash());
    }
}
