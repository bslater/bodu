// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.InvalidBlockSizeHasher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{
    /// <summary>
    /// Test-only hasher whose constructor supplies <c>blockSize = 0</c> to drive the
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}" /> base constructor's non-positive block-size guard.
    /// </summary>
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
}
