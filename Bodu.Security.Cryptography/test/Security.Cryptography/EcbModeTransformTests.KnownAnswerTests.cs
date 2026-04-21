// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // Known-answer vectors — NIST SP 800-38A, Appendix F.1 (ECB mode, AES-128)
    // Source: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38a.pdf
    //
    // ECB does not use an IV; an all-zero placeholder is passed to CreateTransform and ignored.
    public sealed partial class EcbModeTransformTests
    {
        private static readonly byte[] NistKey128
            = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");

        // F.1 common plaintext (4 × 128-bit blocks).
        private static readonly byte[] NistPlaintext
            = Convert.FromHexString(
                "6bc1bee22e409f96e93d7e117393172a" +
                "ae2d8a571e03ac9c9eb76fac45af8e51" +
                "30c81c46a35ce411e5fbc1191a0a52ef" +
                "f69f2445df4f9b17ad2b417be66c3710");

        // ── Test vectors ──────────────────────────────────────────────────────────────────────────

        private static IEnumerable<object[]> EcbKatVectors()
        {
            // F.1.1 ECB-AES128 Encrypt
            yield return new object[]
            {
                "NIST SP 800-38A F.1.1 — ECB-AES128",
                NistKey128,
                new byte[16], // IV placeholder — ECB ignores it
                NistPlaintext,
                Convert.FromHexString(
                    "3ad77bb40d7a3660a89ecaf32466ef97" +
                    "f5d3d58503b9699de785895a96fdbaaf" +
                    "43b1cd7f598ece23881b00e3ed030688" +
                    "7b0c785e27e8ad3f8223207104725dd4"),
            };
        }

        // ── Test methods ──────────────────────────────────────────────────────────────────────────

        [TestMethod]
        [DynamicData(nameof(EcbKatVectors), DynamicDataSourceType.Method)]
        public void Transform_WithNistVector_ShouldEncryptCorrectly(
            string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
            => AssertKatEncrypt(description, key, iv, plaintext, expectedCiphertext);

        [TestMethod]
        [DynamicData(nameof(EcbKatVectors), DynamicDataSourceType.Method)]
        public void Transform_WithNistVector_ShouldDecryptToOriginalPlaintext(
            string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
            => AssertKatDecrypt(description, key, iv, plaintext, expectedCiphertext);
    }
}