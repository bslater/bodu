// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public sealed partial class CbcModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" /> in decrypt mode inverts the CBC chain, XOR-unwrapping each ciphertext block against the prior ciphertext to recover the original plaintext.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldApplyCBCUnchaining()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00); // Identity
        byte[] iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();
        CbcModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        // Encrypted block1 = plaintext1 ^ IV, encrypted block2 = plaintext2 ^ encrypted block1
        byte[] plaintext1 = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
        byte[] plaintext2 = Enumerable.Repeat((byte)0x44, ExpectedBlockSize).ToArray();
        byte[] block1 = plaintext1.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        byte[] block2 = plaintext2.Zip(block1, (a, b) => (byte)(a ^ b)).ToArray();

        byte[] ciphertext = block1.Concat(block2).ToArray();
        byte[] output = new byte[ciphertext.Length];

        transform.Transform(ciphertext, output, encrypt: false);

        CollectionAssert.AreEqual(plaintext1, output[..ExpectedBlockSize].ToArray(), "Decryption of first block failed.");
        CollectionAssert.AreEqual(plaintext2, output[ExpectedBlockSize..].ToArray(), "Decryption of second block failed.");
    }

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" /> in encrypt mode XORs each plaintext block with the prior ciphertext (or IV for block 0) before calling the cipher - the defining chaining property of CBC.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldApplyCBCChaining()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00); // Identity cipher
        byte[] iv = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)(i << 4)).ToArray();
        CbcModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] output = new byte[plaintext.Length];

        transform.Transform(plaintext, output, encrypt: true);

        byte[] expectedBlock1 = plaintext[..ExpectedBlockSize].Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        byte[] expectedBlock2 = plaintext[ExpectedBlockSize..].Zip(expectedBlock1, (a, b) => (byte)(a ^ b)).ToArray();

        CollectionAssert.AreEqual(expectedBlock1, output[..ExpectedBlockSize].ToArray());
        CollectionAssert.AreEqual(expectedBlock2, output[ExpectedBlockSize..].ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" /> in encrypt mode leaves the caller's IV buffer untouched (the transform copies the IV rather than mutating it in place).
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldNotMutateIV()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0xAB, ExpectedBlockSize).ToArray();
        byte[] ivCopy = (byte[])iv.Clone();
        CbcModeTransform transform = CreateTransform(cipher, iv);

        byte[] plaintext = Enumerable.Repeat((byte)0xCD, ExpectedBlockSize).ToArray();
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(ivCopy, iv, "IV must not be mutated after encryption.");
    }

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" />, with SingleBlock, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WithSingleBlock_ShouldEncryptCorrectly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0x11, ExpectedBlockSize).ToArray();
        CbcModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        byte[] expected = plaintext.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        CollectionAssert.AreEqual(expected, output);
    }

    // ── Long input, streamed ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that encryption over <see cref="AesBlockCipher" /> matches the platform's CBC for AES-128 and AES-256,
    /// whether the input arrives in one call or in several calls that split runs.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncryptingLongInput_ShouldMatchPlatformCbc()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength);
            byte[] iv = AesReference.RandomBytes(16, keyLength + 1);
            using System.Security.Cryptography.Aes platform = AesReference.Create(key);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] plaintext = AesReference.RandomBytes(length, length + 3);
                byte[] expected = platform.EncryptCbc(plaintext, iv, System.Security.Cryptography.PaddingMode.None);

                foreach ((string shape, int[] splits) in StreamShapes(length))
                    CollectionAssert.AreEqual(expected, TransformWithAes(key, iv, plaintext, encrypt: true, splits, inPlace: false), $"AES-{keyLength * 8}, {length} bytes, {shape}");
            }
        }
    }

    /// <summary>
    /// Verifies that decryption over <see cref="AesBlockCipher" /> matches the platform's CBC for AES-128 and AES-256,
    /// whether the input arrives in one call or in several calls that split runs.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecryptingLongInput_ShouldMatchPlatformCbc()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength + 11);
            byte[] iv = AesReference.RandomBytes(16, keyLength + 12);
            using System.Security.Cryptography.Aes platform = AesReference.Create(key);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] ciphertext = AesReference.RandomBytes(length, length + 13);
                byte[] expected = platform.DecryptCbc(ciphertext, iv, System.Security.Cryptography.PaddingMode.None);

                foreach ((string shape, int[] splits) in StreamShapes(length))
                    CollectionAssert.AreEqual(expected, TransformWithAes(key, iv, ciphertext, encrypt: false, splits, inPlace: false), $"AES-{keyLength * 8}, {length} bytes, {shape}");
            }
        }
    }

    /// <summary>
    /// Verifies that encrypting a buffer in place over <see cref="AesBlockCipher" /> matches the platform's CBC for AES-128 and
    /// AES-256.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncryptingInPlace_ShouldMatchPlatformCbc()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength + 31);
            byte[] iv = AesReference.RandomBytes(16, keyLength + 32);
            using System.Security.Cryptography.Aes platform = AesReference.Create(key);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] plaintext = AesReference.RandomBytes(length, length + 33);
                byte[] expected = platform.EncryptCbc(plaintext, iv, System.Security.Cryptography.PaddingMode.None);

                CollectionAssert.AreEqual(expected, TransformWithAes(key, iv, plaintext, encrypt: true, [length], inPlace: true), $"AES-{keyLength * 8}, {length} bytes");
            }
        }
    }

    /// <summary>
    /// Verifies that decrypting a buffer in place over <see cref="AesBlockCipher" /> matches the platform's CBC for AES-128 and
    /// AES-256.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecryptingInPlace_ShouldMatchPlatformCbc()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength + 41);
            byte[] iv = AesReference.RandomBytes(16, keyLength + 42);
            using System.Security.Cryptography.Aes platform = AesReference.Create(key);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] ciphertext = AesReference.RandomBytes(length, length + 43);
                byte[] expected = platform.DecryptCbc(ciphertext, iv, System.Security.Cryptography.PaddingMode.None);

                CollectionAssert.AreEqual(expected, TransformWithAes(key, iv, ciphertext, encrypt: false, [length], inPlace: true), $"AES-{keyLength * 8}, {length} bytes");
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
        using var transform = new CbcModeTransform(cipher, (byte[])iv.Clone());
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
