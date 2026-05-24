// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.PaddedFinalBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.GetCurrentHashCore(System.Span{byte})" />
    /// surfaces a clear <see cref="InvalidOperationException" /> when a derived implementation returns a padded
    /// final block whose length is not a multiple of <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" />
    /// while <c>AllowUnalignedFinalBlock</c> is <see langword="false" /> — rather than letting a downstream
    /// span-out-of-range surface and obscure the root cause.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenPadBlockReturnsUnalignedLengthAndAllowUnalignedFinalBlockIsFalse_ShouldThrowInvalidOperationException()
    {
        UnalignedPadBlockHasher hasher = new();
        hasher.Append(new byte[] { 0xAA });

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = hasher.GetCurrentHash());
    }

    /// <summary>
    /// A test-only block hasher that returns a padded final block whose length is deliberately not aligned to
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" /> and leaves <c>AllowUnalignedFinalBlock</c>
    /// at its default of <see langword="false" /> — exercising the guard added by D4.
    /// </summary>
    private sealed class UnalignedPadBlockHasher
        : BlockNonCryptographicHashAlgorithm<UnalignedPadBlockHasher>
    {

        public UnalignedPadBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        protected override UnalignedPadBlockHasher Clone()
        {
            UnalignedPadBlockHasher clone = new();
            clone.CopyResidualStateFrom(this);
            return clone;
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) =>
            new byte[BlockSizeBytes + 1];

        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
        }

        protected override byte[] ProcessFinalBlock() => new byte[4];

    }

}
