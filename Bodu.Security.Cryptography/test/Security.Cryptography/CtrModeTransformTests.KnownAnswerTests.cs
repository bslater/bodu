// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CtrModeTransformTests
{
    // ── NIST SP 800-38A Appendix F.5.1 — AES-128-CTR ─────────────────────────────────────────
    //
    // Key:           2b7e151628aed2a6abf7158809cf4f3c
    // Initial counter (T1): f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff
    // Plaintext blocks P1–P4 / Ciphertext blocks C1–C4.

    private static readonly byte[] NistKey = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
    private static readonly byte[] NistCounter = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");

    private static IEnumerable<object[]> CtrKatVectors()
    {
        // P1 only.
        yield return new object[]
        {
            "NIST F.5.1 P1",
            NistKey,
            NistCounter,
            Convert.FromHexString("6bc1bee22e409f96e93d7e117393172a"),
            Convert.FromHexString("874d6191b620e3261bef6864990db6ce")
        };
        // P1 + P2.
        yield return new object[]
        {
            "NIST F.5.1 P1+P2",
            NistKey,
            NistCounter,
            Convert.FromHexString("6bc1bee22e409f96e93d7e117393172a" +
                                  "ae2d8a571e03ac9c9eb76fac45af8e51"),
            Convert.FromHexString("874d6191b620e3261bef6864990db6ce" +
                                  "9806f66b7970fdff8617187bb9fffdff")
        };
        // P1 + P2 + P3 + P4 (full vector).
        yield return new object[]
        {
            "NIST F.5.1 full (P1-P4)",
            NistKey,
            NistCounter,
            Convert.FromHexString("6bc1bee22e409f96e93d7e117393172a" +
                                  "ae2d8a571e03ac9c9eb76fac45af8e51" +
                                  "30c81c46a35ce411e5fbc1191a0a52ef" +
                                  "f69f2445df4f9b17ad2b417be66c3710"),
            Convert.FromHexString("874d6191b620e3261bef6864990db6ce" +
                                  "9806f66b7970fdff8617187bb9fffdff" +
                                  "5ae4df3edbd5d35e5b4f09020db03eab" +
                                  "1e031dda2fbe03d1792170a0f3009cee")
        };
    }

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" />, with NistVector, EncryptCorrectly.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(CtrKatVectors))]
    public void Transform_WithNistVector_ShouldEncryptCorrectly(
        string description, byte[] key, byte[] counter, byte[] plaintext, byte[] expectedCiphertext)
        => AssertKatEncrypt(description, key, counter, plaintext, expectedCiphertext);

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" />, with NistVector, DecryptToOriginalPlaintext.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(CtrKatVectors))]
    public void Transform_WithNistVector_ShouldDecryptToOriginalPlaintext(
        string description, byte[] key, byte[] counter, byte[] plaintext, byte[] expectedCiphertext)
        => AssertKatDecrypt(description, key, counter, plaintext, expectedCiphertext);
}
