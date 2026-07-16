// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.InvalidBlockSizeHasher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Test-only hasher whose constructor supplies <c>blockSize = 0</c> to drive the
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}" /> base constructor's non-positive block-size guard.
    /// </summary>
    private sealed class InvalidBlockSizeHasher
        : BlockNonCryptographicHashAlgorithm
    {

        public InvalidBlockSizeHasher()
            : base(hashLengthInBytes: 4, blockSize: 0)
        {
        }

        protected override InvalidBlockSizeHasher Clone() => this;

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) => [];

        protected override void ProcessBlock(ReadOnlySpan<byte> block) { }

        protected override byte[] ProcessFinalBlock() => [];

    }

}
