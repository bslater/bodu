// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.PadBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests that exercise the protected <see cref="Fletcher{TSelf}.PadBlock(System.ReadOnlySpan{byte}, ulong)" />
/// path. The production Fletcher variants opt out of final-block padding via
/// <c>ShouldPadFinalBlock() =&gt; false</c>, so <see cref="Fletcher{TSelf}.PadBlock(System.ReadOnlySpan{byte}, ulong)" />
/// is unreachable through the regular surface and must be driven by a test-only subclass that re-enables padding.
/// </summary>
public abstract partial class FletcherTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that <see cref="Fletcher{TSelf}.PadBlock(System.ReadOnlySpan{byte}, ulong)" /> produces a buffer
    /// sized to one block and preserves the residual byte at the head of that block.
    /// </summary>
    [TestMethod]
    public void PadBlock_WhenInvokedDirectly_ShouldReturnSingleByteBlockWithResidualPrefix()
    {
        PaddingFletcher fletcher = new();
        byte[] residual = { 0xAA };

        var padded = fletcher.PadBlockExposed(residual, messageLength: 1);

        Assert.AreEqual(1, padded.Length); // Fletcher is processed byte-wise.
        Assert.AreEqual(0xAA, padded[0]);
    }
    /// <summary>
    /// Verifies that enabling final-block padding on a Fletcher derivative — and forcing a residual into the
    /// buffer — drives <see cref="Fletcher{TSelf}.PadBlock(System.ReadOnlySpan{byte}, ulong)" /> without throwing
    /// and produces a hash of the configured length.
    /// </summary>
    [TestMethod]
    public void PadBlock_WhenInvokedThroughGetCurrentHash_ShouldPadResidualAndProduceHash()
    {
        PaddingFletcher fletcher = new();

        // Three bytes against Fletcher-32's 2-byte block size leaves a 1-byte residual, forcing
        // GetCurrentHashCore down the padding branch.
        fletcher.Append(new byte[] { 0x01, 0x02, 0x03 });

        var hash = fletcher.GetCurrentHash();

        Assert.AreEqual(4, hash.Length);
    }

    /// <summary>
    /// Test-only Fletcher subclass that re-enables final-block padding so that
    /// <see cref="Fletcher{TSelf}.PadBlock(System.ReadOnlySpan{byte}, ulong)" /> is reachable through the standard
    /// hash flow. Also exposes <see cref="PadBlockExposed(byte[], ulong)" /> for direct assertion without going via
    /// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" />.
    /// </summary>
    private sealed class PaddingFletcher
        : Fletcher<PaddingFletcher>
    {

        public PaddingFletcher()
            : base(32)
        {
        }

        public byte[] PadBlockExposed(byte[] residual, ulong messageLength) =>
            this.PadBlock(residual, messageLength);

        protected override bool ShouldPadFinalBlock() => true;

    }

}
