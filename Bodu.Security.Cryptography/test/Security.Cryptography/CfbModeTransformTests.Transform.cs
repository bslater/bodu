// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public sealed partial class CfbModeTransformTests
{
    /// <summary>
    /// Verifies that CFB encryption computes <c>Cᵢ = Pᵢ ⊕ E(IVᵢ)</c> with <c>IV₀</c> equal to the caller-supplied IV and
    /// <c>IVᵢ₊₁ = Cᵢ</c> for subsequent blocks. Uses an identity cipher (xorMask 0x00) so that <c>E(x) = x</c> and the
    /// expected output reduces to successive XOR chains.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldApplyCfbChaining()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();
        CfbModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] output = new byte[plaintext.Length];

        transform.Transform(plaintext, output, encrypt: true);

        // With an identity cipher: C_0 = P_0 ⊕ E(IV) = P_0 ⊕ IV; then C_1 = P_1 ⊕ E(C_0) = P_1 ⊕ C_0.
        byte[] expectedBlock1 = plaintext[..ExpectedBlockSize].Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        byte[] expectedBlock2 = plaintext[ExpectedBlockSize..].Zip(expectedBlock1, (a, b) => (byte)(a ^ b)).ToArray();

        CollectionAssert.AreEqual(expectedBlock1, output[..ExpectedBlockSize].ToArray(), "First CFB block did not match expected chaining output.");
        CollectionAssert.AreEqual(expectedBlock2, output[ExpectedBlockSize..].ToArray(), "Second CFB block did not match expected chaining output.");
    }

    /// <summary>
    /// Verifies that CFB decryption inverts <see cref="Transform_WhenEncrypting_ShouldApplyCfbChaining" />, recovering
    /// the original plaintext for both blocks under the same key and IV.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldApplyCfbUnchaining()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();

        CfbModeTransform encrypt = CreateTransform(cipher, (byte[])iv.Clone());
        CfbModeTransform decrypt = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] recovered = new byte[plaintext.Length];

        encrypt.Transform(plaintext, ciphertext, encrypt: true);
        decrypt.Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered, "CFB decryption did not recover the original plaintext.");
    }

    /// <summary>
    /// Verifies that encrypting in CFB mode does not mutate the caller-supplied initialisation vector.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldNotMutateIv()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = Enumerable.Repeat((byte)0x7E, ExpectedBlockSize).ToArray();
        byte[] ivCopy = (byte[])iv.Clone();
        CfbModeTransform transform = CreateTransform(cipher, iv);

        byte[] plaintext = new byte[ExpectedBlockSize];
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(ivCopy, iv, "CFB must not mutate the caller-supplied IV array.");
    }

    /// <summary>
    /// Verifies that CFB uses the cipher's encrypt primitive on both encryption and decryption paths. This is a defining
    /// property of CFB (it turns the block cipher into a self-synchronising stream cipher).
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldUseCipherEncryptPrimitive()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = Enumerable.Repeat((byte)0x01, ExpectedBlockSize).ToArray();
        CfbModeTransform transform = CreateTransform(cipher, iv);

        byte[] input = new byte[ExpectedBlockSize * 2];
        byte[] output = new byte[input.Length];

        transform.Transform(input, output, encrypt: false);

        Assert.AreEqual(2, cipher.EncryptBlockCount, "CFB decryption must use the cipher's encrypt primitive for every block.");
        Assert.AreEqual(0, cipher.DecryptBlockCount, "CFB decryption must never call the cipher's decrypt primitive.");
    }

    /// <summary>
    /// Verifies that transforming a single full block in CFB mode produces the expected <c>P ⊕ E(IV)</c> output.
    /// </summary>
    [TestMethod]
    public void Transform_WithSingleBlock_ShouldEncryptCorrectly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0x55, ExpectedBlockSize).ToArray();
        CfbModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        byte[] expected = plaintext.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        CollectionAssert.AreEqual(expected, output);
    }

    // ── Long input, streamed ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that encryption over <see cref="AesBlockCipher" /> matches the platform's full-block CFB for AES-128 and AES-256,
    /// whether the input arrives in one call or in several calls that split runs.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncryptingLongInput_ShouldMatchPlatformCfb128()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength);
            byte[] iv = AesReference.RandomBytes(16, keyLength + 1);
            using System.Security.Cryptography.Aes platform = AesReference.Create(key);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] plaintext = AesReference.RandomBytes(length, length + 3);
                byte[] expected = platform.EncryptCfb(plaintext, iv, System.Security.Cryptography.PaddingMode.None, feedbackSizeInBits: 128);

                foreach ((string shape, int[] splits) in StreamShapes(length))
                    CollectionAssert.AreEqual(expected, TransformWithAes(key, iv, plaintext, encrypt: true, splits, inPlace: false), $"AES-{keyLength * 8}, {length} bytes, {shape}");
            }
        }
    }

    /// <summary>
    /// Verifies that decryption over <see cref="AesBlockCipher" /> matches the platform's full-block CFB for AES-128 and AES-256,
    /// whether the input arrives in one call or in several calls that split runs.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecryptingLongInput_ShouldMatchPlatformCfb128()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength + 11);
            byte[] iv = AesReference.RandomBytes(16, keyLength + 12);
            using System.Security.Cryptography.Aes platform = AesReference.Create(key);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] ciphertext = AesReference.RandomBytes(length, length + 13);
                byte[] expected = platform.DecryptCfb(ciphertext, iv, System.Security.Cryptography.PaddingMode.None, feedbackSizeInBits: 128);

                foreach ((string shape, int[] splits) in StreamShapes(length))
                    CollectionAssert.AreEqual(expected, TransformWithAes(key, iv, ciphertext, encrypt: false, splits, inPlace: false), $"AES-{keyLength * 8}, {length} bytes, {shape}");
            }
        }
    }

    /// <summary>
    /// Verifies that encrypting a buffer in place over <see cref="AesBlockCipher" /> matches the platform's full-block CFB for AES-128 and
    /// AES-256.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncryptingInPlace_ShouldMatchPlatformCfb128()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength + 31);
            byte[] iv = AesReference.RandomBytes(16, keyLength + 32);
            using System.Security.Cryptography.Aes platform = AesReference.Create(key);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] plaintext = AesReference.RandomBytes(length, length + 33);
                byte[] expected = platform.EncryptCfb(plaintext, iv, System.Security.Cryptography.PaddingMode.None, feedbackSizeInBits: 128);

                CollectionAssert.AreEqual(expected, TransformWithAes(key, iv, plaintext, encrypt: true, [length], inPlace: true), $"AES-{keyLength * 8}, {length} bytes");
            }
        }
    }

    /// <summary>
    /// Returns ways to split an input across <see cref="IBlockCipherModeTransform.Transform" /> calls: one call, and
    /// calls that end one block into a 4 KiB run and one block short of one.
    /// </summary>
    /// <param name="length">The block-aligned input length.</param>
    /// <returns>Each shape's name and its call lengths.</returns>
    private static IEnumerable<(string Shape, int[] Splits)> StreamShapes(int length)
    {
        yield return ("one call", [length]);
        if (length > 32)
            yield return ("calls of 16, rest", [16, length - 16]);

        if (length > 4112)
            yield return ("calls of 4112, 16, rest", [4112, 16, length - 4128]);
    }

    /// <summary>
    /// Transforms an input with the mode over <see cref="AesBlockCipher" />, in the given call lengths.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="iv">The initialization vector.</param>
    /// <param name="input">The input.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt; <see langword="false" /> to decrypt.</param>
    /// <param name="splits">The length of each call.</param>
    /// <param name="inPlace"><see langword="true" /> to transform the input buffer in place.</param>
    /// <returns>The output.</returns>
    private static byte[] TransformWithAes(byte[] key, byte[] iv, byte[] input, bool encrypt, int[] splits, bool inPlace)
    {
        using var cipher = new AesBlockCipher(key);
        using var transform = new CfbModeTransform(cipher, (byte[])iv.Clone());
        byte[] buffer = (byte[])input.Clone();
        byte[] output = inPlace ? buffer : new byte[input.Length];
        int offset = 0;
        foreach (int split in splits)
        {
            transform.Transform(buffer.AsSpan(offset, split), output.AsSpan(offset, split), encrypt);
            offset += split;
        }

        return output;
    }
}
