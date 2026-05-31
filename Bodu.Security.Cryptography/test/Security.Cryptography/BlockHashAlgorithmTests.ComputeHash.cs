// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockHashAlgorithmTests.ComputeHash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that feeding the same input across differently sized streaming chunks yields the same final hash as
    /// hashing the input in a single pass.
    /// </summary>
    /// <remarks>
    /// Exercises the residual-buffer logic in <see cref="BlockHashAlgorithm{T}" /> to confirm that arbitrarily sized
    /// transform segments are correctly accumulated into aligned blocks before being dispatched to
    /// <c>ProcessBlock</c>. Input is derived deterministically from the specification's <see cref="HashAlgorithmSpecification.InputBlockSize" />
    /// to guarantee coverage of sub-block, exact-block, and multi-block boundaries.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public virtual void ComputeHash_WhenInputStreamedInChunks_ShouldMatchSinglePassResult(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);

        // Cover sub-block, exact-block, and multi-block boundaries deterministically.
        var inputLength = (specification.InputBlockSize * 3) + (specification.InputBlockSize / 2) + 1;
        var input = Enumerable.Range(0, inputLength)
                                 .Select(i => (byte)((i * 31) + 7))
                                 .ToArray();

        byte[] expected;
        using (TAlgorithm reference = CreateAlgorithm(variant))
            expected = reference.ComputeHash(input);

        foreach (var chunkSize in StreamingChunkSizes(specification))
        {
            using TAlgorithm algorithm = CreateAlgorithm(variant);
            var offset = 0;

            while (offset < input.Length)
            {
                var count = Math.Min(chunkSize, input.Length - offset);
                algorithm.TransformBlock(input, offset, count, null, 0);
                offset += count;
            }

            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            CollectionAssert.AreEqual(
                expected,
                algorithm.Hash!,
                $"[{variant}] Streaming with chunk size {chunkSize} diverged from the single-pass result.");
        }
    }

    /// <summary>
    /// Verifies that inputs whose length is not a multiple of the block size are padded and
    /// processed without error, producing a hash of the expected output length.
    /// </summary>
    /// <remarks>
    /// Verifies that <see cref="BlockHashAlgorithm{T}.PadBlock" /> handles the full residual-length
    /// domain on the happy path and that <see cref="BlockHashAlgorithm{T}.ProcessFinalBlock" />
    /// produces a digest of the size advertised by <see cref="HashAlgorithm.HashSize" />.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public virtual void ComputeHash_WhenInputIsUnaligned_ShouldProduceValidHash(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);
        int expectedLength;
        using (TAlgorithm reference = CreateAlgorithm())
        {
            expectedLength = reference.HashSize / 8;
        }

        foreach (var length in UnalignedInputLengths(specification))
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            var input = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();

            var hash = algorithm.ComputeHash(input);

            Assert.IsNotNull(hash, $"Hash for unaligned input length {length} must not be null.");
            Assert.AreEqual(
                expectedLength,
                hash.Length,
                $"Hash length for unaligned input length {length} did not match HashSize.");
        }
    }

    /// <summary>
    /// Verifies that hashing an input whose length is exactly a multiple of the block size produces the same result
    /// regardless of whether the input is supplied in a single pass or across successive block-aligned transform
    /// calls.
    /// </summary>
    /// <remarks>
    /// Isolates the block-aligned fast path so that residual buffering cannot mask a regression in block dispatch
    /// ordering or per-block state accumulation. Input is derived deterministically using the same pattern as other
    /// entropy tests in the suite.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public virtual void ComputeHash_WhenInputIsBlockAligned_ShouldMatchAcrossTransformSplits(TVariant variant)
    {
        const int blockCount = 4;
        HashAlgorithmSpecification specification = GetSpecification(variant);
        var blockSize = specification.InputBlockSize;

        var input = Enumerable.Range(0, blockSize * blockCount)
                                 .Select(i => (byte)((i * 31) + 7))
                                 .ToArray();

        byte[] expected;
        using (TAlgorithm reference = CreateAlgorithm(variant))
            expected = reference.ComputeHash(input);

        using TAlgorithm algorithm = CreateAlgorithm(variant);

        for (var i = 0; i < blockCount; i++)
            algorithm.TransformBlock(input, i * blockSize, blockSize, null, 0);

        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(
            expected,
            algorithm.Hash!,
            $"[{variant}] Block-aligned transform splits diverged from the single-pass result.");
    }
}
