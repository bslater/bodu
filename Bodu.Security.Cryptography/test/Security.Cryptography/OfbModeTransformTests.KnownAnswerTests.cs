// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfbModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    // Known-answer vectors — NIST SP 800-38A, Appendix F.4.1 (OFB mode, AES-128)
    // Source: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38a.pdf
    //
    // OFB is self-inverse: the same operation is applied for both encryption and decryption.
    // The decrypt KAT therefore calls Transform with encrypt: false and expects the original
    // plaintext, which is the same as re-encrypting the ciphertext.
    public sealed partial class OfbModeTransformTests
    {
        private static readonly byte[] OfbNistKey128
            = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");

        private static readonly byte[] OfbNistIv
            = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");

        private static readonly byte[] OfbNistPlaintext
            = Convert.FromHexString(
                "6bc1bee22e409f96e93d7e117393172a" +
                "ae2d8a571e03ac9c9eb76fac45af8e51" +
                "30c81c46a35ce411e5fbc1191a0a52ef" +
                "f69f2445df4f9b17ad2b417be66c3710");

        private static IEnumerable<object[]> OfbKatVectors()
        {
            yield return new object[]
            {
                "NIST SP 800-38A F.4.1 — OFB-AES128",
                OfbNistKey128,
                OfbNistIv,
                OfbNistPlaintext,
                Convert.FromHexString(
                    "3b3fd92eb72dad20333449f8e83cfb4a" +
                    "7789508d16918f03f53c52dac54ed825" +
                    "9740051e9c5fecf64344f7a82260edcc" +
                    "304c6528f659c77866a510d9c1d6ae5e"),
            };
        }

        [TestMethod]
        [DynamicData(nameof(OfbKatVectors), DynamicDataSourceType.Method)]
        public void Transform_WithNistVector_ShouldEncryptCorrectly(
            string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
            => AssertKatEncrypt(description, key, iv, plaintext, expectedCiphertext);

        [TestMethod]
        [DynamicData(nameof(OfbKatVectors), DynamicDataSourceType.Method)]
        public void Transform_WithNistVector_ShouldDecryptToOriginalPlaintext(
            string description, byte[] key, byte[] iv, byte[] plaintext, byte[] expectedCiphertext)
            => AssertKatDecrypt(description, key, iv, plaintext, expectedCiphertext);
    }
}