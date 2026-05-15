// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.RecordingBlockHasher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
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

        public void CopyFromExposed(BlockNonCryptographicHashAlgorithm<RecordingBlockHasher>? source)
            => CopyResidualStateFrom(source!);

        protected override RecordingBlockHasher Clone()
        {
            RecordingBlockHasher clone = new();
            clone.Blocks.AddRange(Blocks);
            clone.CopyResidualStateFrom(this);
            return clone;
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
            => throw new InvalidOperationException("PadBlock should not be reached: ShouldPadFinalBlock is false.");

        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            Blocks.Add(block.ToArray());
        }

        protected override byte[] ProcessFinalBlock() => new byte[4];

        protected override bool ShouldPadFinalBlock() => false;

    }

}
