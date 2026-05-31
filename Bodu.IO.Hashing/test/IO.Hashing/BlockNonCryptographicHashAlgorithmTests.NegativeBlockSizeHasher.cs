// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.NegativeBlockSizeHasher.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Test-only hasher whose constructor supplies a negative <c>blockSize</c> to drive the
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}" /> base constructor's non-positive block-size guard.
    /// </summary>
    private sealed class NegativeBlockSizeHasher
        : BlockNonCryptographicHashAlgorithm<NegativeBlockSizeHasher>
    {

        public NegativeBlockSizeHasher()
            : base(hashLengthInBytes: 4, blockSize: -1)
        {
        }

        protected override NegativeBlockSizeHasher Clone() => this;

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) => [];

        protected override void ProcessBlock(ReadOnlySpan<byte> block) { }

        protected override byte[] ProcessFinalBlock() => [];

    }

}
