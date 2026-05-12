// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.PaddingBlockHasher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{
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
            var output = new byte[BlockSizeBytes * 2];
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
}
