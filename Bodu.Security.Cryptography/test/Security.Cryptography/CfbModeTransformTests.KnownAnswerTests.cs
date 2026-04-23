// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

// Known-answer vectors — NIST SP 800-38A, Appendix F.3.13 (CFB128 mode, AES-128)
// Source: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38a.pdf
//
// Note: NIST CFB128 uses the full block as the feedback unit (segment size s = 128), which
// is the natural full-block CFB implemented here.
public sealed partial class CfbModeTransformTests
{
    private static readonly byte[] CfbNistKey128
        = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");

    private static readonly byte[] CfbNistIv
        = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");

    private static readonly byte[] CfbNistPlaintext
        = Convert.FromHexString(
            "6bc1bee22e409f96e93d7e117393172a" +
            "ae2d8a571e03ac9c9eb76fac45af8e51" +
            "30c81c46a35ce411e5fbc1191a0a52ef" +
            "f69f2445df4f9b17ad2b417be66c3710");

    private static IEnumerable<object[]> CfbKatVectors()
    {
        yield return new object[]
        {
            "NIST SP 800-38A F.3.13 — CFB128-AES128",
            CfbNistKey128,
            CfbNistIv,
            CfbNistPlaintext,
            Convert.FromHexString(
                "3b3fd92eb72dad20333449f8e83cfb4a" +
                "c8a64537a0b3a93fcde3cdad9f1ce58b" +
                "26751f67a3cbb140b1808cf187a4f4df" +
                "c04b05357c5d1c0eeac4c66f9ff7f2e6"),
        };
    }

    /// <summary>
    /// Verifies that <see cref="CfbModeTransform.Transform" />, with NistVector, EncryptCorrectly.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CfbKatVectors), DynamicDataSourceType.Method)]
    public void Transform_WithNistVector_ShouldEncryptCorrectly(
        string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
        => AssertKatEncrypt(description, key, iv, plaintext, expectedCiphertext);

    /// <summary>
    /// Verifies that <see cref="CfbModeTransform.Transform" />, with NistVector, DecryptToOriginalPlaintext.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CfbKatVectors), DynamicDataSourceType.Method)]
    public void Transform_WithNistVector_ShouldDecryptToOriginalPlaintext(
        string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
        => AssertKatDecrypt(description, key, iv, plaintext, expectedCiphertext);
}
