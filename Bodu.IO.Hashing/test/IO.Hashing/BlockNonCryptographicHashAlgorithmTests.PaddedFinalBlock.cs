// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.PaddedFinalBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.GetCurrentHashCore(System.Span{byte})" />
    /// accepts a padded final block whose length is not a multiple of
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" /> when the derived implementation opts in
    /// by setting <c>AllowUnalignedFinalBlock</c> to <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenPadBlockReturnsUnalignedLengthAndAllowUnalignedFinalBlockIsTrue_ShouldComputeHashWithoutThrowing()
    {
        AllowedUnalignedPadBlockHasher hasher = new();
        hasher.Append(new byte[] { 0xAA });

        byte[] hash = hasher.GetCurrentHash();

        Assert.IsNotNull(hash);
        Assert.HasCount(4, hash);
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

    /// <summary>
    /// A test-only block hasher that returns a padded final block whose length is deliberately not aligned to
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" /> and opts in via
    /// <c>AllowUnalignedFinalBlock</c> — exercising the accept path of the same guard.
    /// </summary>
    private sealed class AllowedUnalignedPadBlockHasher
        : BlockNonCryptographicHashAlgorithm<AllowedUnalignedPadBlockHasher>
    {

        public AllowedUnalignedPadBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        protected override bool AllowUnalignedFinalBlock => true;

        protected override AllowedUnalignedPadBlockHasher Clone()
        {
            AllowedUnalignedPadBlockHasher clone = new();
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
