// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.UnalignedPaddingBlockHasher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// A padded hasher identical to <see cref="PaddingBlockHasher" /> except that it overrides
    /// <c>AllowUnalignedFinalBlock</c> to <see langword="true" />, exercising the code path where
    /// <c>GetCurrentHashCore</c> forwards the entire padded buffer to <c>ProcessBlock</c> in a single call
    /// rather than slicing it into block-sized chunks.
    /// </summary>
    private sealed class UnalignedPaddingBlockHasher
        : BlockNonCryptographicHashAlgorithm
    {

        public readonly List<byte[]> Blocks = new();

        public UnalignedPaddingBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        protected override bool AllowUnalignedFinalBlock => true;

        protected override UnalignedPaddingBlockHasher Clone()
        {
            UnalignedPaddingBlockHasher clone = new();
            clone.Blocks.AddRange(Blocks);
            clone.CopyResidualStateFrom(this);
            return clone;
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
        {
            byte[] output = new byte[BlockSizeBytes + 3];
            block.CopyTo(output);
            output[BlockSizeBytes] = unchecked((byte)messageLength);
            return output;
        }

        protected override void ProcessBlock(ReadOnlySpan<byte> block) => Blocks.Add(block.ToArray());

        protected override byte[] ProcessFinalBlock() => new byte[4];

    }

}
