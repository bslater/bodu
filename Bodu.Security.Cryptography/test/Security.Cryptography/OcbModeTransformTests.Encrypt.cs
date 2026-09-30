// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class OcbModeTransformTests
{
    // ── Output length - non-default tag sizes ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Encrypt" /> writes exactly
    /// <c>|PT| + (tagSize / 8)</c> bytes to the output buffer when a non-default tag size
    /// is used, and that the return value equals the same quantity.
    /// </summary>
    [TestMethod]
    [DataRow(64, DisplayName = "tagSize = 64 bits")]
    [DataRow(96, DisplayName = "tagSize = 96 bits")]
    [DataRow(128, DisplayName = "tagSize = 128 bits")]
    public void Encrypt_WithGivenTagSize_OutputShouldBePlaintextLengthPlusTagBytes(int tagSize)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        byte[] plaintext = new byte[ExpectedBlockSize];
        OcbModeTransform transform = CreateTransform(cipher, new byte[ExpectedBlockSize], tagSize);
        int tagBytes = tagSize / 8;
        byte[] output = new byte[plaintext.Length + tagBytes];

        int written = transform.Encrypt(plaintext, output);

        Assert.AreEqual(plaintext.Length + tagBytes, written,
            $"Encrypt must return |PT| + (tagSize / 8) bytes (tagSize = {tagSize} bits).");
    }

    // ── Domain separation ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that changing the tag length produces completely different ciphertext
    /// and tag, confirming OCB3's intentional domain separation across TAGLEN configurations.
    /// </summary>
    /// <remarks>
    /// RFC 7253 §2.4 encodes TAGLEN as <c>num2str(TAGLEN mod 128, 7)</c> in the first
    /// seven bits of the nonce word: for TAGLEN=128 byte 0 is <c>0x00</c>; for TAGLEN=96
    /// it is <c>0xC0</c>. The different nonce word produces a different Ktop, Stretch, and
    /// Offset_0, so the entire computation diverges from the first cipher call onwards.
    /// This design prevents a forger from truncating a valid 128-bit ciphertext to produce
    /// a valid 96-bit authentication - the two computations are entirely unrelated.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithDifferentTagLengths_ShouldProduceDifferentCiphertextAndTag()
    {
        using var cipher16 = new AesBlockCipherFixture(new byte[16]);
        using var cipher12 = new AesBlockCipherFixture(new byte[16]);
        byte[] iv = new byte[ExpectedBlockSize];
        byte[] plaintext = new byte[ExpectedBlockSize];

        OcbModeTransform enc16 = CreateTransform(cipher16, (byte[])iv.Clone(), 128);
        OcbModeTransform enc12 = CreateTransform(cipher12, (byte[])iv.Clone(), 96);
        byte[] ct16 = new byte[plaintext.Length + 16];
        byte[] ct12 = new byte[plaintext.Length + 12];
        enc16.Encrypt(plaintext, ct16);
        enc12.Encrypt(plaintext, ct12);

        CollectionAssert.AreNotEqual(
            ct16.Take(plaintext.Length).ToArray(),
            ct12.Take(plaintext.Length).ToArray(),
            "Different TAGLEN values produce different nonce words and therefore " +
            "different ciphertext (RFC 7253 §2.4 domain separation).");

        CollectionAssert.AreNotEqual(
            ct16.Skip(plaintext.Length).Take(12).ToArray(),
            ct12.Skip(plaintext.Length).Take(12).ToArray(),
            "Tags computed under different TAGLEN values must differ (no prefix relationship).");
    }

    // ── Key sensitivity ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that encrypting the same plaintext and AAD under two different keys produces
    /// different ciphertext and tag.
    /// </summary>
    /// <remarks>
    /// This is a basic key-sensitivity sanity check. Because the L values (<c>L_*</c>,
    /// <c>L_$</c>, <c>L[i]</c>) and the K_top all depend on the key, a different key
    /// changes every intermediate value in both the HASH and the encryption pass.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithDifferentKeys_ShouldProduceDifferentOutput()
    {
        using var cipher1 = new AesBlockCipherFixture(new byte[16]);
        using var cipher2 = new AesBlockCipherFixture(Enumerable.Repeat((byte)0xFF, 16).ToArray());
        byte[] iv = new byte[ExpectedBlockSize];
        byte[] plaintext = new byte[ExpectedBlockSize];

        var enc1 = new OcbModeTransform(cipher1, iv);
        byte[] ct1 = new byte[plaintext.Length + (enc1.TagSize / 8)];
        enc1.Encrypt(plaintext, ct1);

        var enc2 = new OcbModeTransform(cipher2, iv);
        byte[] ct2 = new byte[plaintext.Length + (enc2.TagSize / 8)];
        enc2.Encrypt(plaintext, ct2);

        CollectionAssert.AreNotEqual(ct1, ct2,
            "Different keys must produce different ciphertext and tag.");
    }

    // ── Long messages ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the ciphertext and tag match a block-at-a-time RFC 7253 reference on the platform's AES for AES-128
    /// and AES-256, across message lengths that fit in one run of blocks, straddle one, and span several, with
    /// associated data of every alignment.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenMessageSpansSeveralRuns_ShouldMatchBlockAtATimeReference()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength);
            byte[] nonce = AesReference.RandomBytes(12, keyLength + 1);

            foreach (int length in AesReference.MessageLengths)
            {
                foreach (int aadLength in AesReference.AssociatedDataLengths)
                {
                    byte[] plaintext = AesReference.RandomBytes(length, length + 7);
                    byte[] aad = AesReference.RandomBytes(aadLength, aadLength + 11);

                    using var cipher = new AesBlockCipher(key);
                    using var transform = new OcbModeTransform(cipher, ReferenceIv(nonce));
                    transform.ProcessAssociatedData(aad);
                    byte[] actual = new byte[length + 16];
                    transform.Encrypt(plaintext, actual);

                    CollectionAssert.AreEqual(ReferenceSeal(key, nonce, aad, plaintext), actual, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }

    /// <summary>
    /// Seals a message with OCB (RFC 7253, 128-bit tag) one block at a time.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <param name="aad">The associated data.</param>
    /// <param name="plaintext">The plaintext.</param>
    /// <returns>The ciphertext followed by the tag.</returns>
    private static byte[] ReferenceSeal(byte[] key, byte[] nonce, byte[] aad, byte[] plaintext)
    {
        using System.Security.Cryptography.Aes aes = AesReference.Create(key);
        byte[] lStar = AesReference.Encrypt(aes, new byte[16]);
        byte[] lDollar = AesReference.Double(lStar);
        var l = new List<byte[]> { AesReference.Double(lDollar) };
        byte[] L(int i)
        {
            while (l.Count <= i)
                l.Add(AesReference.Double(l[^1]));

            return l[i];
        }

        // Section 4.2: Nonce = num2str(TAGLEN mod 128, 7) || zeros(120 - bitlen(N)) || 1 || N, here 0^31 1 || N.
        byte[] nonceBlock = new byte[16];
        nonceBlock[3] = 0x01;
        nonce.CopyTo(nonceBlock, 4);
        int bottom = nonceBlock[15] & 0x3F;
        byte[] ktopInput = (byte[])nonceBlock.Clone();
        ktopInput[15] &= 0xC0;
        byte[] ktop = AesReference.Encrypt(aes, ktopInput);
        byte[] stretch = [.. ktop, .. AesReference.Xor(ktop[..8], ktop[1..9])];
        var stretchValue = new System.Numerics.BigInteger(stretch, isUnsigned: true, isBigEndian: true);
        byte[] offsetBytes = ((stretchValue >> (64 - bottom)) & ((System.Numerics.BigInteger.One << 128) - 1)).ToByteArray(isUnsigned: true, isBigEndian: true);
        byte[] offset = new byte[16];
        offsetBytes.CopyTo(offset, 16 - offsetBytes.Length);

        byte[] checksum = new byte[16];
        byte[] output = new byte[plaintext.Length + 16];
        int fullBlocks = plaintext.Length / 16;
        for (int i = 1; i <= fullBlocks; i++)
        {
            byte[] block = plaintext[(16 * (i - 1))..(16 * i)];
            offset = AesReference.Xor(offset, L(System.Numerics.BitOperations.TrailingZeroCount(i)));
            AesReference.Xor(offset, AesReference.Encrypt(aes, AesReference.Xor(block, offset))).CopyTo(output, 16 * (i - 1));
            checksum = AesReference.Xor(checksum, block);
        }

        int remainder = plaintext.Length % 16;
        if (remainder > 0)
        {
            offset = AesReference.Xor(offset, lStar);
            byte[] pad = AesReference.Encrypt(aes, offset);
            byte[] padded = new byte[16];
            for (int i = 0; i < remainder; i++)
            {
                output[(16 * fullBlocks) + i] = (byte)(plaintext[(16 * fullBlocks) + i] ^ pad[i]);
                padded[i] = plaintext[(16 * fullBlocks) + i];
            }

            padded[remainder] = 0x80;
            checksum = AesReference.Xor(checksum, padded);
        }

        byte[] tag = AesReference.Xor(AesReference.Encrypt(aes, AesReference.Xor(AesReference.Xor(checksum, offset), lDollar)), ReferenceHash(aes, lStar, L, aad));
        tag.CopyTo(output, plaintext.Length);
        return output;
    }

    /// <summary>
    /// Computes OCB's <c>HASH(K, A)</c> (RFC 7253 Section 4.1) one block at a time.
    /// </summary>
    /// <param name="aes">The keyed instance.</param>
    /// <param name="lStar">The value <c>L_*</c>.</param>
    /// <param name="l">Returns <c>L_i</c>.</param>
    /// <param name="aad">The associated data.</param>
    /// <returns>The 16-byte hash.</returns>
    private static byte[] ReferenceHash(System.Security.Cryptography.Aes aes, byte[] lStar, Func<int, byte[]> l, byte[] aad)
    {
        byte[] sum = new byte[16];
        byte[] offset = new byte[16];
        int fullBlocks = aad.Length / 16;
        for (int i = 1; i <= fullBlocks; i++)
        {
            offset = AesReference.Xor(offset, l(System.Numerics.BitOperations.TrailingZeroCount(i)));
            sum = AesReference.Xor(sum, AesReference.Encrypt(aes, AesReference.Xor(aad[(16 * (i - 1))..(16 * i)], offset)));
        }

        int remainder = aad.Length % 16;
        if (remainder > 0)
        {
            offset = AesReference.Xor(offset, lStar);
            byte[] padded = new byte[16];
            Array.Copy(aad, 16 * fullBlocks, padded, 0, remainder);
            padded[remainder] = 0x80;
            sum = AesReference.Xor(sum, AesReference.Encrypt(aes, AesReference.Xor(padded, offset)));
        }

        return sum;
    }

    /// <summary>
    /// Returns the 16-byte initialization vector whose first twelve bytes are <paramref name="nonce" />.
    /// </summary>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <returns>The initialization vector.</returns>
    private static byte[] ReferenceIv(byte[] nonce)
    {
        byte[] iv = new byte[16];
        nonce.CopyTo(iv, 0);
        return iv;
    }
}
