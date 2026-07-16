// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests{T,T}.BlockBoundary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class FletcherTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that appending inputs around the Fletcher block boundary (a spread of lengths covering residual-only,
    /// exact-block, block-plus-one, and multi-block scenarios) produces identical digests whether the input is
    /// submitted in a single call, in three roughly equal chunks, or one byte at a time — exercising the residual
    /// buffer state machine inside <see cref="BlockNonCryptographicHashAlgorithm{T}.Append" />.
    /// </summary>
    /// <param name="length">The input length under test, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(63)]
    [DataRow(64)]
    [DataRow(65)]
    public void Append_WhenInputStraddlesBlockBoundary_ShouldMatchAcrossSubmissionStyles(int length)
    {
        byte[] input = new byte[length];
        for (int i = 0; i < length; i++)
            input[i] = (byte)i;

        TAlgorithm whole = CreateAlgorithm();
        whole.Append(input);

        TAlgorithm chunked = CreateAlgorithm();
        int third = length / 3;
        chunked.Append(input.AsSpan(0, third));
        chunked.Append(input.AsSpan(third, third));
        chunked.Append(input.AsSpan(third * 2, length - (third * 2)));

        TAlgorithm perByte = CreateAlgorithm();
        foreach (byte b in input)
            perByte.Append(new[] { b });

        byte[] wholeHash = whole.GetCurrentHash();
        CollectionAssert.AreEqual(wholeHash, chunked.GetCurrentHash(),
            $"Chunked append produced a different digest than single-shot for length {length}.");
        CollectionAssert.AreEqual(wholeHash, perByte.GetCurrentHash(),
            $"Byte-at-a-time append produced a different digest than single-shot for length {length}.");
    }

    /// <summary>
    /// Verifies that a large multi-batch input produces identical digests whether submitted in one call, in odd-sized
    /// chunks that straddle the internal batch boundary, or one byte at a time. This pins the deferred-modulo
    /// accumulation across internal block boundaries so a batched rewrite of the inner loop stays digest-identical.
    /// </summary>
    /// <param name="length">The input length under test, in bytes.</param>
    [TestMethod]
    [DataRow(4095)]
    [DataRow(4096)]
    [DataRow(4097)]
    [DataRow(8192)]
    [DataRow(10000)]
    public void Append_WhenInputSpansManyBatches_ShouldMatchAcrossSubmissionStyles(int length)
    {
        byte[] input = new byte[length];
        for (int i = 0; i < length; i++)
            input[i] = (byte)((i * 31) + 7);

        TAlgorithm whole = CreateAlgorithm();
        whole.Append(input);

        TAlgorithm chunked = CreateAlgorithm();
        int pos = 0;
        foreach (int chunk in new[] { 1, 63, 4096, 500, 3000 })
        {
            if (pos >= length) break;
            int take = Math.Min(chunk, length - pos);
            chunked.Append(input.AsSpan(pos, take));
            pos += take;
        }
        if (pos < length)
            chunked.Append(input.AsSpan(pos));

        TAlgorithm perByte = CreateAlgorithm();
        foreach (byte b in input)
            perByte.Append(new[] { b });

        byte[] wholeHash = whole.GetCurrentHash();
        CollectionAssert.AreEqual(wholeHash, chunked.GetCurrentHash(),
            $"Chunked append produced a different digest than single-shot for length {length}.");
        CollectionAssert.AreEqual(wholeHash, perByte.GetCurrentHash(),
            $"Byte-at-a-time append produced a different digest than single-shot for length {length}.");
    }

    /// <summary>
    /// Verifies that <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is
    /// non-destructive: calling it mid-stream, appending additional data, and calling it again must yield the
    /// same digest as the same sequence computed on a fresh instance without any intermediate observation.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenCalledMidStream_ShouldNotDisturbSubsequentHashing()
    {
        byte[] input = new byte[32];
        for (int i = 0; i < input.Length; i++)
            input[i] = (byte)(i ^ 0xAA);

        TAlgorithm observed = CreateAlgorithm();
        observed.Append(input.AsSpan(0, 16));
        _ = observed.GetCurrentHash();
        observed.Append(input.AsSpan(16));

        TAlgorithm reference = CreateAlgorithm();
        reference.Append(input);

        CollectionAssert.AreEqual(reference.GetCurrentHash(), observed.GetCurrentHash());
    }

}
