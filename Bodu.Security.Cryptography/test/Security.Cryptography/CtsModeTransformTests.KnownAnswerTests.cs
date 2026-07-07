// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtsModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

// No widely-cited single-authority CTS test vectors exist: NIST SP 800-38A Addendum
// acknowledges three CS variants (CS1/CS2/CS3) without mandating specific test cases.
// CtsModeTransform implements the CS3 variant (ciphertext swap, CBC-based).
//
// The inherited Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip test uses
// block-aligned input (which degenerates to plain CBC). The additional test below
// specifically exercises the steal path with a non-aligned plaintext.
public sealed partial class CtsModeTransformTests
{
    // ── AES-128 CBC-CS3 known-answer tests ────────────────────────────────────────────────────
    //
    // The SP 800-38A Addendum specifies CBC-CS1/CS2/CS3 but publishes no worked AES vectors, so
    // these rows are DERIVED: the CBC-CS3 algorithm from the Addendum applied to the official NIST
    // AES-128 CBC example inputs from SP 800-38A Appendix F.2.1 (Key 2B7E..., IV 000102...). They
    // are deterministic algorithm-over-standard-input derivations, not pasted NIST ciphertext, and
    // pin the ciphertext-stealing data path (17-, 31-, and 47-byte plaintexts: 1 block + tail,
    // 2 blocks - tail, 3 blocks - tail). CS3 always swaps the final two ciphertext blocks.

    /// <summary>
    /// Verifies that <see cref="CtsModeTransform" /> encrypting under AES-128 produces exactly the
    /// CBC-CS3 ciphertext derived from the NIST SP 800-38A F.2.1 inputs, and that decryption recovers
    /// the plaintext — pinning the ciphertext-stealing data path a symmetric round-trip cannot.
    /// </summary>
    /// <param name="keyHex">The AES-128 key.</param>
    /// <param name="ivHex">The 16-byte initialization vector.</param>
    /// <param name="plaintextHex">The non-block-aligned plaintext.</param>
    /// <param name="ciphertextHex">The expected CBC-CS3 ciphertext.</param>
    [TestMethod]
    [DataRow("2B7E151628AED2A6ABF7158809CF4F3C", "000102030405060708090A0B0C0D0E0F",
        "6BC1BEE22E409F96E93D7E117393172AAE",
        "B8D266C62A614F00D7C901DC791ECEA976")]
    [DataRow("2B7E151628AED2A6ABF7158809CF4F3C", "000102030405060708090A0B0C0D0E0F",
        "6BC1BEE22E409F96E93D7E117393172AAE2D8A571E03AC9C9EB76FAC45AF8E",
        "47937B55F8652154C6E9A6F35BAFBB567649ABAC8119B246CEE98E9B12E919")]
    [DataRow("2B7E151628AED2A6ABF7158809CF4F3C", "000102030405060708090A0B0C0D0E0F",
        "6BC1BEE22E409F96E93D7E117393172AAE2D8A571E03AC9C9EB76FAC45AF8E5130C81C46A35CE411E5FBC1191A0A52",
        "7649ABAC8119B246CEE98E9B12E9197D9DC43313F849E395374EAC6507D949B75086CB9B507219EE95DB113A917678")]
    public void Transform_WithNistCbcCs3DerivedVector_ShouldMatchAndRoundTrip(
        string keyHex, string ivHex, string plaintextHex, string ciphertextHex)
    {
        byte[] key = Convert.FromHexString(keyHex);
        byte[] iv = Convert.FromHexString(ivHex);
        byte[] plaintext = Convert.FromHexString(plaintextHex);
        byte[] expected = Convert.FromHexString(ciphertextHex);

        using var cipher = new AesBlockCipherFixture(key);

        byte[] ciphertext = new byte[plaintext.Length];
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(plaintext, ciphertext, encrypt: true);

        CollectionAssert.AreEqual(expected, ciphertext,
            "CTS encryption must match the NIST-derived CBC-CS3 ciphertext.");

        byte[] recovered = new byte[plaintext.Length];
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered,
            "CTS decryption must recover the plaintext from the CBC-CS3 ciphertext.");
    }

    /// <summary>
    /// Verifies that the CTS steal path round-trips correctly under a real AES cipher
    /// using a non-block-aligned plaintext (3.5 blocks). The base-class round-trip test
    /// uses block-aligned input (which degenerates to plain CBC); this test specifically
    /// exercises the ciphertext-stealing branch of the implementation.
    /// </summary>
    [TestMethod]
    public void Transform_WithRealAesCipher_NonAlignedInput_ShouldRoundTrip()
    {
        byte[] key = new byte[16];
        RandomNumberGenerator.Fill(key);

        using var cipher = new AesBlockCipherFixture(key);

        // 3 full blocks + half a block to force the steal path. Convert from bits to bytes.
        int blockBytes = cipher.BlockSize / 8;
        int length = blockBytes * 3 + blockBytes / 2;
        byte[] iv = new byte[blockBytes];
        byte[] plaintext = new byte[length];
        RandomNumberGenerator.Fill(iv);
        RandomNumberGenerator.Fill(plaintext);

        byte[] ciphertext = new byte[length];
        byte[] recovered = new byte[length];

        CreateTransform(cipher, (byte[])iv.Clone()).Transform(plaintext, ciphertext, encrypt: true);
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered,
            "CTS steal path must recover non-aligned plaintext under a real AES cipher.");
    }
}
