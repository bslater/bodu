// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests{T,T}.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public abstract partial class MurmurHash3Tests<TTest, TAlgorithm>
{
    /// <summary>
    /// Builds the deterministic reference message used by the streaming-equivalence tests. The length (257 bytes) is
    /// deliberately not a multiple of either variant's block size so every split exercises the tail path.
    /// </summary>
    /// <returns>The reference message.</returns>
    private static byte[] BuildStreamingReferenceMessage()
    {
        var message = new byte[257];
        for (int i = 0; i < message.Length; i++)
            message[i] = (byte)((i * 31) + 7);

        return message;
    }

    /// <summary>
    /// Verifies that appending the reference message in fixed-size chunks produces exactly the digest of a single
    /// one-shot append, across chunk sizes that straddle the variant's block boundaries.
    /// </summary>
    /// <param name="chunkSize">The chunk size to split the message into.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(64)]
    public void Append_WhenInputSplitIntoChunks_ShouldMatchOneShotDigest(int chunkSize)
    {
        byte[] message = BuildStreamingReferenceMessage();

        using TAlgorithm oneShot = CreateAlgorithm(SingleTestVariant.Default);
        oneShot.Append(message);
        byte[] expected = oneShot.GetCurrentHash();

        using TAlgorithm chunked = CreateAlgorithm(SingleTestVariant.Default);
        for (int offset = 0; offset < message.Length; offset += chunkSize)
            chunked.Append(message.AsSpan(offset, Math.Min(chunkSize, message.Length - offset)));

        CollectionAssert.AreEqual(expected, chunked.GetCurrentHash(),
            $"Chunked append (size {chunkSize}) diverged from the one-shot digest.");
    }

    /// <summary>
    /// Verifies that reading the current hash mid-stream is non-destructive: appends made after the read continue the
    /// original computation and yield the same final digest as an uninterrupted run.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenReadMidStream_ShouldNotDisturbSubsequentAppends()
    {
        byte[] message = BuildStreamingReferenceMessage();
        int split = 101;

        using TAlgorithm uninterrupted = CreateAlgorithm(SingleTestVariant.Default);
        uninterrupted.Append(message);
        byte[] expected = uninterrupted.GetCurrentHash();

        using TAlgorithm interrupted = CreateAlgorithm(SingleTestVariant.Default);
        interrupted.Append(message.AsSpan(0, split));
        _ = interrupted.GetCurrentHash();
        interrupted.Append(message.AsSpan(split));

        CollectionAssert.AreEqual(expected, interrupted.GetCurrentHash(),
            "A mid-stream digest read disturbed the running computation.");
    }

    /// <summary>
    /// Verifies that <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" /> after streamed input restores
    /// the initial state, so a subsequent computation matches a fresh instance.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalledAfterStreamedInput_ShouldRestoreInitialState()
    {
        byte[] message = BuildStreamingReferenceMessage();

        using TAlgorithm fresh = CreateAlgorithm(SingleTestVariant.Default);
        fresh.Append(message);
        byte[] expected = fresh.GetCurrentHash();

        using TAlgorithm reused = CreateAlgorithm(SingleTestVariant.Default);
        reused.Append(message.AsSpan(0, 57));
        reused.Reset();
        reused.Append(message);

        CollectionAssert.AreEqual(expected, reused.GetCurrentHash(),
            "Reset after streamed input did not restore the initial state.");
    }
}
