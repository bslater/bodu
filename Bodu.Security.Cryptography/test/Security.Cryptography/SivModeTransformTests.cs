// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="SivModeTransform" /> (RFC 5297 — AES-SIV with CMAC and S2V).
/// </summary>
[TestClass]
public sealed partial class SivModeTransformTests
    : AeadBlockCipherModeTests<SivModeTransformTests, SivModeTransform>
{
    protected override int ExpectedBlockSize => 16;

    /// <inheritdoc />
    /// <remarks>
    /// SIV (RFC 5297, as implemented here) ignores the supplied IV: the synthetic IV is derived
    /// deterministically from the key, AAD, and plaintext via S2V/CMAC. The ciphertext therefore
    /// does not vary with the input IV and the nonce-variation tests do not apply.
    /// </remarks>
    protected override bool NonceAffectsCiphertext => false;

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="CreateTransform" /> does not forward the <paramref name="cipher" /> argument to the
    /// production constructor — it uses fixed AES keys for both the S2V and CTR ciphers. Passing
    /// <see langword="null" /> therefore never reaches the null-check in
    /// <see cref="SivModeTransform" />, so the base-class cipher-null test is not applicable here.
    /// </remarks>
    protected override bool ValidatesCipherArgument => false;

    // Fixed keys used for all structural / tamper-detection tests.
    // Using real AES with distinct K1 and K2 prevents the degenerate authentication fixed-point
    // that arises when a simple XOR cipher is used for both S2V (K1) and CTR (K2): with any
    // XOR cipher, tampering the SIV causes CTR to produce a new "plaintext" whose S2V equals
    // the tampered SIV, so FixedTimeEquals always passes and no CryptographicException is thrown.
    private static readonly byte[] s_s2vTestKey =
        Convert.FromHexString("fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0"); // K1 — matches RFC 5297 A.1
    private static readonly byte[] s_ctrTestKey =
        Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"); // K2 — matches RFC 5297 A.1

    /// <summary>
    /// Creates a <see cref="SivModeTransform" /> backed by two distinct AES-128 ciphers
    /// (K₁ for S2V, K₂ for CTR). The <paramref name="cipher" /> parameter is accepted to satisfy
    /// the abstract base-class contract but is not used; real AES is required here because a simple
    /// XOR test cipher creates a degenerate authentication case for SIV (see field comments above).
    /// </summary>
    protected override SivModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new(
            s2vCipher: new AesBlockCipherFixture(s_s2vTestKey),
            ctrCipher: new AesBlockCipherFixture(s_ctrTestKey),
            iv: iv);
}
