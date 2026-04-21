namespace Bodu.Security.Cryptography
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    /// <summary>
    /// Tests for <see cref="SivModeTransform" /> (RFC 5297 — AES-SIV with CMAC and S2V).
    /// </summary>
    [TestClass]
    public sealed partial class SivModeTransformTests : AeadBlockCipherModeTests<SivModeTransform>
    {
        protected override int ExpectedBlockSize => 16;

        // Fixed keys used for all structural / tamper-detection tests.
        // Using real AES with distinct K1 and K2 prevents the degenerate authentication fixed-point
        // that arises when a simple XOR cipher is used for both S2V (K1) and CTR (K2): with any
        // XOR cipher, tampering the SIV causes CTR to produce a new "plaintext" whose S2V equals
        // the tampered SIV, so FixedTimeEquals always passes and no CryptographicException is thrown.
        private static readonly byte[] S2vTestKey =
            Convert.FromHexString("fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0"); // K1 — matches RFC 5297 A.1
        private static readonly byte[] CtrTestKey =
            Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"); // K2 — matches RFC 5297 A.1

        /// <summary>
        /// Creates a <see cref="SivModeTransform" /> backed by two distinct AES-128 ciphers
        /// (K₁ for S2V, K₂ for CTR). The <paramref name="cipher" /> parameter is accepted to satisfy
        /// the abstract base-class contract but is not used; real AES is required here because a simple
        /// XOR test cipher creates a degenerate authentication case for SIV (see field comments above).
        /// </summary>
        protected override SivModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new SivModeTransform(
                s2vCipher: new AesBlockCipherFixture(S2vTestKey),
                ctrCipher: new AesBlockCipherFixture(CtrTestKey),
                iv: iv);
    }
}