// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

// Known-answer vectors — NIST SP 800-38A, Appendix F.2.1 (CBC mode, AES-128)
// Source: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38a.pdf
public sealed partial class CbcModeTransformTests
{
    private static readonly byte[] CbcNistKey128
        = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");

    private static readonly byte[] CbcNistIv
        = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");

    private static readonly byte[] CbcNistPlaintext
        = Convert.FromHexString(
            "6bc1bee22e409f96e93d7e117393172a" +
            "ae2d8a571e03ac9c9eb76fac45af8e51" +
            "30c81c46a35ce411e5fbc1191a0a52ef" +
            "f69f2445df4f9b17ad2b417be66c3710");

    private static IEnumerable<object[]> CbcKatVectors()
    {
        yield return new object[]
        {
            "NIST SP 800-38A F.2.1 — CBC-AES128",
            CbcNistKey128,
            CbcNistIv,
            CbcNistPlaintext,
            Convert.FromHexString(
                "7649abac8119b246cee98e9b12e9197d" +
                "5086cb9b507219ee95db113a917678b2" +
                "73bed6b8e3c1743b7116e69e22229516" +
                "3ff1caa1681fac09120eca307586e1a7"),
        };
    }

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" />, with NistVector, EncryptCorrectly.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CbcKatVectors))]
    public void Transform_WithNistVector_ShouldEncryptCorrectly(
        string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
        => AssertKatEncrypt(description, key, iv, plaintext, expectedCiphertext);

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" />, with NistVector, DecryptToOriginalPlaintext.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CbcKatVectors))]
    public void Transform_WithNistVector_ShouldDecryptToOriginalPlaintext(
        string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
        => AssertKatDecrypt(description, key, iv, plaintext, expectedCiphertext);
}
