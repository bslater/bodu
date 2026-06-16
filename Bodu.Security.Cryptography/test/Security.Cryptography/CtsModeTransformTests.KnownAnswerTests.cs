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
