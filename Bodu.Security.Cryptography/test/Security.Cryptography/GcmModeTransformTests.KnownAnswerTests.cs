// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

// Known-answer vectors — NIST SP 800-38D, Appendix B Test Case 4 (AES-128-GCM, no AAD)
// Source: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38d.pdf
//
// GcmModeTransform takes the initial counter block J0 directly as its 16-byte IV.
// For NIST 96-bit nonces the caller derives J0 = nonce || 0x00000001 before construction.
// Test Case 4 uses a 96-bit nonce "cafebabefacedbaddecaf888", so:
//   IV passed to GcmModeTransform = cafebabefacedbaddecaf888_00000001
public sealed partial class GcmModeTransformTests
{
    private static IEnumerable<object[]> GcmKatVectors()
    {
        // TC4 — 128-bit key, 96-bit nonce, 64-byte plaintext, empty AAD.
        yield return new object[]
        {
        "NIST SP 800-38D TC4 — AES-128-GCM (no AAD)",
        Convert.FromHexString("feffe9928665731c6d6a8f9467308308"),         // key
        Convert.FromHexString("cafebabefacedbaddecaf888"),                 // nonce
        Array.Empty<byte>(),                                               // aad
        Convert.FromHexString(                                             // plaintext
            "d9313225f88406e5a55909c5aff5269a" +
            "86a7a9531534f7da2e4c303d8a318a72" +
            "1c3c0c95956809532fcf0e2449a6b525" +
            "b16aedf5aa0de657ba637b391aafd255"),
        Convert.FromHexString(                                             // expected ciphertext
            "42831ec2217774244b7221b784d0d49c" +
            "e3aa212f2c02a4e035c17e2329aca12e" +
            "21d514b25466931c7d8f6a5aac84aa05" +
            "1ba30b396a0aac973d58e091473f5985"),
        Convert.FromHexString("4d5c2af327cd64a62cf35abd2ba6fab4"),         // expected tag
        };

        // TC7 — 128-bit key, 96-bit nonce, 60-byte plaintext, 20-byte AAD.
        yield return new object[]
        {
        "NIST SP 800-38D TC7 — AES-128-GCM (with AAD)",
        Convert.FromHexString("feffe9928665731c6d6a8f9467308308"),         // key
        Convert.FromHexString("cafebabefacedbaddecaf888"),                 // nonce
        Convert.FromHexString("feedfacedeadbeeffeedfacedeadbeefabaddad2"), // aad
        Convert.FromHexString(                                             // plaintext
            "d9313225f88406e5a55909c5aff5269a" +
            "86a7a9531534f7da2e4c303d8a318a72" +
            "1c3c0c95956809532fcf0e2449a6b525" +
            "b16aedf5aa0de657ba637b39"),
        Convert.FromHexString(                                             // expected ciphertext
            "42831ec2217774244b7221b784d0d49c" +
            "e3aa212f2c02a4e035c17e2329aca12e" +
            "21d514b25466931c7d8f6a5aac84aa05" +
            "1ba30b396a0aac973d58e091"),
        Convert.FromHexString("5bc94fbc3221a5db94fae95ae7121a47"),         // expected tag
        };

        // TC1 — all-zero 128-bit key, zero nonce, empty plaintext, empty AAD. Exercises the
        // GHASH length block and tag path with no ciphertext folded in.
        yield return new object[]
        {
        "NIST SP 800-38D TC1 — AES-128-GCM (empty plaintext, empty AAD)",
        Convert.FromHexString("00000000000000000000000000000000"),         // key
        Convert.FromHexString("000000000000000000000000"),                 // nonce
        Array.Empty<byte>(),                                               // aad
        Array.Empty<byte>(),                                               // plaintext
        Array.Empty<byte>(),                                               // expected ciphertext
        Convert.FromHexString("58e2fccefa7e3061367f1d57a4e7455a"),         // expected tag
        };

        // TC2 — all-zero key/nonce, single all-zero plaintext block, empty AAD.
        yield return new object[]
        {
        "NIST SP 800-38D TC2 — AES-128-GCM (one zero block)",
        Convert.FromHexString("00000000000000000000000000000000"),         // key
        Convert.FromHexString("000000000000000000000000"),                 // nonce
        Array.Empty<byte>(),                                               // aad
        Convert.FromHexString("00000000000000000000000000000000"),         // plaintext
        Convert.FromHexString("0388dace60b6a392f328c2b971b2fe78"),         // expected ciphertext
        Convert.FromHexString("ab6e47d42cec13bdf53a67b21257bddf"),         // expected tag
        };

        // TC15 — 256-bit key, 96-bit nonce, 64-byte plaintext, empty AAD.
        yield return new object[]
        {
        "NIST SP 800-38D TC15 — AES-256-GCM (no AAD)",
        Convert.FromHexString(                                             // key
            "feffe9928665731c6d6a8f9467308308" +
            "feffe9928665731c6d6a8f9467308308"),
        Convert.FromHexString("cafebabefacedbaddecaf888"),                 // nonce
        Array.Empty<byte>(),                                               // aad
        Convert.FromHexString(                                             // plaintext
            "d9313225f88406e5a55909c5aff5269a" +
            "86a7a9531534f7da2e4c303d8a318a72" +
            "1c3c0c95956809532fcf0e2449a6b525" +
            "b16aedf5aa0de657ba637b391aafd255"),
        Convert.FromHexString(                                             // expected ciphertext
            "522dc1f099567d07f47f37a32a84427d" +
            "643a8cdcbfe5c0c97598a2bd2555d1aa" +
            "8cb08e48590dbb3da7b08b1056828838" +
            "c5f61e6393ba7a0abcc9f662898015ad"),
        Convert.FromHexString("b094dac5d93471bdec1a502270e3cc6c"),         // expected tag
        };

        // TC16 — 256-bit key, 96-bit nonce, 60-byte plaintext, 20-byte AAD.
        yield return new object[]
        {
        "NIST SP 800-38D TC16 — AES-256-GCM (with AAD)",
        Convert.FromHexString(                                             // key
            "feffe9928665731c6d6a8f9467308308" +
            "feffe9928665731c6d6a8f9467308308"),
        Convert.FromHexString("cafebabefacedbaddecaf888"),                 // nonce
        Convert.FromHexString("feedfacedeadbeeffeedfacedeadbeefabaddad2"), // aad
        Convert.FromHexString(                                             // plaintext
            "d9313225f88406e5a55909c5aff5269a" +
            "86a7a9531534f7da2e4c303d8a318a72" +
            "1c3c0c95956809532fcf0e2449a6b525" +
            "b16aedf5aa0de657ba637b39"),
        Convert.FromHexString(                                             // expected ciphertext
            "522dc1f099567d07f47f37a32a84427d" +
            "643a8cdcbfe5c0c97598a2bd2555d1aa" +
            "8cb08e48590dbb3da7b08b1056828838" +
            "c5f61e6393ba7a0abcc9f662"),
        Convert.FromHexString("76fc6ece0f4e1768cddf8853bb2d551b"),         // expected tag
        };
    }

    public static string GcmKatVectordisplayname(MethodInfo methodInfo, object[] data)
        => $"{methodInfo.Name} — {data[0]}";

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.Encrypt" />, with NistVector, ProduceExpectedCiphertextAndTag.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(GcmKatVectors), DynamicDataDisplayName = nameof(GcmKatVectordisplayname))]
    public void Encrypt_WithNistVector_ShouldProduceExpectedCiphertextAndTag(
        string description, byte[] key, byte[] iv, byte[] aad,
        byte[] plaintext, byte[] expectedCiphertext, byte[] expectedTag)
        => AssertKatEncrypt(description, key, iv, aad, plaintext, expectedCiphertext, expectedTag);

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.Decrypt" />, with NistVector, RecoverOriginalPlaintext.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(GcmKatVectors))]
    public void Decrypt_WithNistVector_ShouldRecoverOriginalPlaintext(
        string description, byte[] key, byte[] iv, byte[] aad,
        byte[] plaintext, byte[] expectedCiphertext, byte[] expectedTag)
        => AssertKatDecrypt(description, key, iv, aad, plaintext, expectedCiphertext, expectedTag);
}
