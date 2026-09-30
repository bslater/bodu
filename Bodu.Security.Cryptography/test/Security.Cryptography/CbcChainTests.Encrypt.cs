// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcChainTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class CbcChainTests
{
    /// <summary>
    /// Verifies that the ciphertext and the final chaining value match the platform's CBC for a cipher that chains
    /// natively, at every length on both sides of the chained-call threshold.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenLengthVaries_ForACipherThatChains_ShouldMatchPlatformCbc()
    {
        byte[] key = AesReference.RandomBytes(16, 71);
        using var cipher = new AesBlockCipher(key);

        AssertMatchesPlatform(cipher, key);
    }

    /// <summary>
    /// Verifies that the ciphertext and the final chaining value match the platform's CBC for a cipher that encrypts
    /// only single blocks.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenLengthVaries_ForACipherThatDoesNotChain_ShouldMatchPlatformCbc()
    {
        byte[] key = AesReference.RandomBytes(16, 72);
        using var cipher = new AesBlockCipherFixture(key);

        AssertMatchesPlatform(cipher, key);
    }

    /// <summary>
    /// Verifies that chains carried from call to call through the chaining value - alternating long chains, which
    /// reset the cipher's cached chain, with short ones - match one platform chain over the whole input.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenChainIsSplitAcrossCalls_ShouldMatchOnePlatformChain()
    {
        byte[] key = AesReference.RandomBytes(16, 73);
        byte[] iv = AesReference.RandomBytes(16, 74);
        byte[] input = AesReference.RandomBytes(16 * 700, 75);
        using Aes platform = AesReference.Create(key);
        byte[] expected = platform.EncryptCbc(input, iv, PaddingMode.None);

        using var cipher = new AesBlockCipher(key);
        byte[] chainingValue = (byte[])iv.Clone();
        byte[] output = new byte[input.Length];
        int offset = 0;
        foreach (int blocks in new[] { 300, 1, 7, 2, 250, 6, 5, 129 })
        {
            CbcChain.Encrypt(cipher, input.AsSpan(offset, 16 * blocks), output.AsSpan(offset, 16 * blocks), chainingValue);
            offset += 16 * blocks;
        }

        CollectionAssert.AreEqual(expected, output);
        CollectionAssert.AreEqual(expected[^16..], chainingValue);
    }

    /// <summary>
    /// Verifies that encrypting in place - the output being exactly the input - gives the platform's ciphertext on both
    /// paths.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenOutputIsTheInput_ShouldMatchPlatformCbc()
    {
        byte[] key = AesReference.RandomBytes(16, 76);
        byte[] iv = AesReference.RandomBytes(16, 77);
        using Aes platform = AesReference.Create(key);
        using var cipher = new AesBlockCipher(key);

        foreach (int length in s_lengths)
        {
            byte[] buffer = AesReference.RandomBytes(length, length + 78);
            byte[] expected = platform.EncryptCbc(buffer, iv, PaddingMode.None);

            CbcChain.Encrypt(cipher, buffer, buffer, (byte[])iv.Clone());

            CollectionAssert.AreEqual(expected, buffer, $"{length} bytes");
        }
    }

    /// <summary>
    /// Verifies that an empty input leaves the chaining value unchanged.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenInputIsEmpty_ShouldLeaveChainingValueUnchanged()
    {
        using var cipher = new AesBlockCipher(new byte[16]);
        byte[] chainingValue = AesReference.RandomBytes(16, 79);
        byte[] original = (byte[])chainingValue.Clone();

        CbcChain.Encrypt(cipher, [], [], chainingValue);

        CollectionAssert.AreEqual(original, chainingValue);
    }

    /// <summary>
    /// Encrypts each test length with <see cref="CbcChain.Encrypt" /> and with the platform's CBC, and asserts that the
    /// ciphertexts and the final chaining values agree.
    /// </summary>
    /// <param name="cipher">The cipher under test, keyed with <paramref name="key" />.</param>
    /// <param name="key">The key.</param>
    private static void AssertMatchesPlatform(IBlockCipher cipher, byte[] key)
    {
        using Aes platform = AesReference.Create(key);
        foreach (int length in s_lengths)
        {
            byte[] iv = AesReference.RandomBytes(16, length);
            byte[] input = AesReference.RandomBytes(length, length + 1);
            byte[] expected = platform.EncryptCbc(input, iv, PaddingMode.None);

            byte[] chainingValue = (byte[])iv.Clone();
            byte[] output = new byte[length];
            CbcChain.Encrypt(cipher, input, output, chainingValue);

            CollectionAssert.AreEqual(expected, output, $"{length} bytes");
            CollectionAssert.AreEqual(expected[^16..], chainingValue, $"{length} bytes, chaining value");
        }
    }
}
