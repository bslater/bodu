// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipherTests.EncryptCbc.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class AesBlockCipherTests
{
    /// <summary>
    /// Verifies that the chained CBC path matches the platform's CBC for each key size, one block to several 4 KiB
    /// chunks, and leaves the last ciphertext block as the chaining value.
    /// </summary>
    /// <param name="keyLength">The key length, in bytes.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void EncryptCbc_WhenGivenWholeBlocks_ShouldMatchPlatformCbc(int keyLength)
    {
        byte[] key = AesReference.RandomBytes(keyLength, keyLength + 91);
        using Aes platform = AesReference.Create(key);
        using var cipher = new AesBlockCipher(key);
        ICbcBlockCipher chained = cipher;

        foreach (int length in new[] { 16, 32, 4080, 4096, 4112, 20000 })
        {
            byte[] iv = AesReference.RandomBytes(16, length + 92);
            byte[] input = AesReference.RandomBytes(length, length + 93);
            byte[] expected = platform.EncryptCbc(input, iv, PaddingMode.None);

            byte[] chainingValue = (byte[])iv.Clone();
            byte[] output = new byte[length];
            chained.EncryptCbc(input, output, chainingValue);

            CollectionAssert.AreEqual(expected, output, $"{length} bytes");
            CollectionAssert.AreEqual(expected[^16..], chainingValue, $"{length} bytes, chaining value");
        }
    }

    /// <summary>
    /// Verifies that the chained CBC path starts every chain from the chaining value it is given, even after a chain
    /// that failed part-way - the cached platform chain is reset after each call.
    /// </summary>
    [TestMethod]
    public void EncryptCbc_WhenCalledAgain_ShouldStartFromTheGivenChainingValue()
    {
        byte[] key = AesReference.RandomBytes(16, 94);
        using Aes platform = AesReference.Create(key);
        using var cipher = new AesBlockCipher(key);
        ICbcBlockCipher chained = cipher;
        byte[] input = AesReference.RandomBytes(4096 + 160, 95);

        foreach (int round in new[] { 1, 2, 3 })
        {
            byte[] iv = AesReference.RandomBytes(16, 96 + round);
            byte[] output = new byte[input.Length];
            chained.EncryptCbc(input, output, (byte[])iv.Clone());

            CollectionAssert.AreEqual(platform.EncryptCbc(input, iv, PaddingMode.None), output, $"chain {round}");
        }
    }

    /// <summary>
    /// Verifies that the chained CBC path throws <see cref="ObjectDisposedException" /> once the cipher is disposed.
    /// </summary>
    [TestMethod]
    public void EncryptCbc_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var cipher = new AesBlockCipher(new byte[16]);
        ICbcBlockCipher chained = cipher;
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            chained.EncryptCbc(new byte[16], new byte[16], new byte[16]);
        });
    }
}
