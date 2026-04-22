// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for <see cref="BlockNonCryptographicHashAlgorithm{T}" /> that exercise branches not
/// reachable through the production <see cref="Fletcher{TSelf}" /> derivatives — specifically the
/// <c>blockSize &lt;= 0</c> constructor guard, the <c>CopyResidualStateFrom(null)</c> guard, and both
/// padded-finalisation code paths governed by <see cref="BlockNonCryptographicHashAlgorithm{T}" />'s
/// <c>ShouldPadFinalBlock</c> and <c>AllowUnalignedFinalBlock</c> virtual members.
/// </summary>
[TestClass]
public class BlockNonCryptographicHashAlgorithmTests
{
    /// <summary>
    /// A test-only block hasher with block size 4 that records every full block passed to
    /// <see cref="ProcessBlock(ReadOnlySpan{byte})" />. <c>ShouldPadFinalBlock</c> defaults to <see langword="false" />
    /// so the final residual is passed through verbatim, matching the Fletcher production path.
    /// </summary>
    private sealed class RecordingBlockHasher
        : BlockNonCryptographicHashAlgorithm<RecordingBlockHasher>
    {
        public readonly List<byte[]> Blocks = new();

        public RecordingBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            Blocks.Add(block.ToArray());
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
            => throw new InvalidOperationException("PadBlock should not be reached: ShouldPadFinalBlock is false.");

        protected override byte[] ProcessFinalBlock() => new byte[4];

        protected override bool ShouldPadFinalBlock() => false;

        protected override RecordingBlockHasher Clone()
        {
            RecordingBlockHasher clone = new();
            clone.Blocks.AddRange(Blocks);
            clone.CopyResidualStateFrom(this);
            return clone;
        }

        public void CopyFromExposed(BlockNonCryptographicHashAlgorithm<RecordingBlockHasher>? source)
            => CopyResidualStateFrom(source!);
    }

    /// <summary>
    /// A test-only block hasher that exercises the <c>ShouldPadFinalBlock = true</c> path. Padding is a simple
    /// zero-fill out to the next block boundary plus a length-encoding block; <see cref="AllowUnalignedFinalBlock" />
    /// defaults to <see langword="false" /> so <c>GetCurrentHashCore</c> slices the padded output into
    /// block-sized chunks before forwarding to <c>ProcessBlock</c>.
    /// </summary>
    private sealed class PaddingBlockHasher
        : BlockNonCryptographicHashAlgorithm<PaddingBlockHasher>
    {
        // Shared across the outer instance and any Clone() snapshots so block invocations performed during
        // GetCurrentHashCore on the clone are observable through the outer instance's Blocks property.
        public List<byte[]> Blocks = new();

        public PaddingBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            Blocks.Add(block.ToArray());
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
        {
            // Return exactly two blocks of BlockSizeBytes — the residual (zero-padded) followed by a
            // length-encoding block. Aligns with the ShouldPadFinalBlock=true, AllowUnalignedFinalBlock=false
            // contract where GetCurrentHashCore slices the output into BlockSizeBytes chunks.
            byte[] output = new byte[BlockSizeBytes * 2];
            block.CopyTo(output);
            output[BlockSizeBytes] = unchecked((byte)messageLength);
            return output;
        }

        protected override byte[] ProcessFinalBlock() => new byte[4];

        protected override PaddingBlockHasher Clone()
        {
            PaddingBlockHasher clone = new();
            clone.Blocks = Blocks;
            clone.CopyResidualStateFrom(this);
            return clone;
        }
    }

    /// <summary>
    /// A padded hasher identical to <see cref="PaddingBlockHasher" /> except that it overrides
    /// <c>AllowUnalignedFinalBlock</c> to <see langword="true" />, exercising the code path where
    /// <c>GetCurrentHashCore</c> forwards the entire padded buffer to <c>ProcessBlock</c> in a single call
    /// rather than slicing it into block-sized chunks.
    /// </summary>
    private sealed class UnalignedPaddingBlockHasher
        : BlockNonCryptographicHashAlgorithm<UnalignedPaddingBlockHasher>
    {
        public readonly List<byte[]> Blocks = new();

        public UnalignedPaddingBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        protected override bool AllowUnalignedFinalBlock => true;

        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            Blocks.Add(block.ToArray());
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
        {
            byte[] output = new byte[BlockSizeBytes + 3];
            block.CopyTo(output);
            output[BlockSizeBytes] = unchecked((byte)messageLength);
            return output;
        }

        protected override byte[] ProcessFinalBlock() => new byte[4];

        protected override UnalignedPaddingBlockHasher Clone()
        {
            UnalignedPaddingBlockHasher clone = new();
            clone.Blocks.AddRange(Blocks);
            clone.CopyResidualStateFrom(this);
            return clone;
        }
    }

    private sealed class InvalidBlockSizeHasher
        : BlockNonCryptographicHashAlgorithm<InvalidBlockSizeHasher>
    {
        public InvalidBlockSizeHasher()
            : base(hashLengthInBytes: 4, blockSize: 0)
        {
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) => Array.Empty<byte>();

        protected override void ProcessBlock(ReadOnlySpan<byte> block) { }

        protected override byte[] ProcessFinalBlock() => Array.Empty<byte>();

        protected override InvalidBlockSizeHasher Clone() => this;
    }

    private sealed class NegativeBlockSizeHasher
        : BlockNonCryptographicHashAlgorithm<NegativeBlockSizeHasher>
    {
        public NegativeBlockSizeHasher()
            : base(hashLengthInBytes: 4, blockSize: -1)
        {
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) => Array.Empty<byte>();

        protected override void ProcessBlock(ReadOnlySpan<byte> block) { }

        protected override byte[] ProcessFinalBlock() => Array.Empty<byte>();

        protected override NegativeBlockSizeHasher Clone() => this;
    }

    /// <summary>
    /// Verifies that the <see cref="BlockNonCryptographicHashAlgorithm{T}" /> constructor throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>blockSize</c> when the
    /// supplied block size is zero.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new InvalidBlockSizeHasher());
        Assert.AreEqual("blockSize", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the <see cref="BlockNonCryptographicHashAlgorithm{T}" /> constructor throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>blockSize</c> when the
    /// supplied block size is negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NegativeBlockSizeHasher());
        Assert.AreEqual("blockSize", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.CopyResidualStateFrom" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> equal to <c>source</c> when invoked with a
    /// <see langword="null" /> source instance.
    /// </summary>
    [TestMethod]
    public void CopyResidualStateFrom_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        RecordingBlockHasher hasher = new();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() => hasher.CopyFromExposed(null));
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that when the residual buffer plus the incoming input exactly fill one block, the combined block
    /// is emitted to <c>ProcessBlock</c> and the residual is cleared.
    /// </summary>
    [TestMethod]
    public void Append_WhenResidualPlusInputExactlyFillsBlock_ShouldEmitOneAlignedBlock()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01, 0x02 });              // residual = 2 bytes
        hasher.Append(new byte[] { 0x03, 0x04 });              // fills the block exactly

        Assert.AreEqual(1, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, hasher.Blocks[0]);
    }

    /// <summary>
    /// Verifies that when the combined residual plus incoming input is smaller than one block, no block is
    /// emitted and the bytes are retained in the residual buffer.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputSmallerThanRemainingCapacity_ShouldAccumulateInResidualOnly()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01 });
        hasher.Append(new byte[] { 0x02 });

        Assert.AreEqual(0, hasher.Blocks.Count);
    }

    /// <summary>
    /// Verifies that an input consisting of multiple full blocks (with no residual) is emitted as one
    /// <c>ProcessBlock</c> invocation per block, in order.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputSpansMultipleFullBlocks_ShouldEmitEachBlockInOrder()
    {
        RecordingBlockHasher hasher = new();
        byte[] input = new byte[]
        {
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C,
        };
        hasher.Append(input);

        Assert.AreEqual(3, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, hasher.Blocks[0]);
        CollectionAssert.AreEqual(new byte[] { 0x05, 0x06, 0x07, 0x08 }, hasher.Blocks[1]);
        CollectionAssert.AreEqual(new byte[] { 0x09, 0x0A, 0x0B, 0x0C }, hasher.Blocks[2]);
    }

    /// <summary>
    /// Verifies that <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" /> clears the residual
    /// buffer accumulated by <c>ProcessBlocks</c> so that the next append cycle starts from an empty residual.
    /// </summary>
    [TestMethod]
    public void Reset_AfterAppend_ShouldClearResidualBuffer()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01, 0x02, 0x03 });  // residual = 3 bytes
        hasher.Reset();

        // After Reset, a subsequent 4-byte append should produce exactly one aligned block consisting of the new
        // bytes only (no carry-over from the discarded residual).
        hasher.Append(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });

        Assert.AreEqual(1, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, hasher.Blocks[0]);
    }

    /// <summary>
    /// Verifies that when <c>ShouldPadFinalBlock</c> is <see langword="true" /> and
    /// <c>AllowUnalignedFinalBlock</c> is <see langword="false" />, <c>GetCurrentHashCore</c> slices the padded
    /// output into <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" />-sized chunks and invokes
    /// <c>ProcessBlock</c> once per chunk.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenShouldPadAndAlignedFinalBlock_ShouldSlicePaddedOutputIntoBlocks()
    {
        PaddingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x11, 0x22 });  // residual = 2 bytes — no blocks emitted yet
        Assert.AreEqual(0, hasher.Blocks.Count);

        _ = hasher.GetCurrentHash();

        // PadBlock returns two blocks of four bytes each; the padded payload must be sliced into exactly two
        // ProcessBlock calls on the cloned instance. Blocks is shared with the clone so the outer instance
        // witnesses the invocations.
        Assert.AreEqual(2, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0x11, 0x22, 0x00, 0x00 }, hasher.Blocks[0]);
        Assert.AreEqual(4, hasher.Blocks[1].Length);
    }

    /// <summary>
    /// Verifies that when <c>AllowUnalignedFinalBlock</c> is <see langword="true" />, <c>GetCurrentHashCore</c>
    /// forwards the full padded buffer (whose length is not necessarily a multiple of
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" />) to <c>ProcessBlock</c> in a single
    /// call — the cloned instance must not throw despite the unaligned length.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenShouldPadAndUnalignedFinalBlockIsAllowed_ShouldForwardPaddedBufferWholesale()
    {
        UnalignedPaddingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x11, 0x22 });

        // The padded buffer returned by PadBlock is 7 bytes, which is not a multiple of the 4-byte block size.
        // With AllowUnalignedFinalBlock = true this must still succeed without throwing.
        byte[] digest = hasher.GetCurrentHash();

        Assert.IsNotNull(digest);
        Assert.AreEqual(4, digest.Length);
    }
}
