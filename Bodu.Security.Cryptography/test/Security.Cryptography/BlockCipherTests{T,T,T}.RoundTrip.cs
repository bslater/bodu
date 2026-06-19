// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests{T,T,T}.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
{
    /// <summary>
    /// Verifies that encrypting an arbitrary plaintext block and then decrypting the resulting ciphertext
    /// through the same cipher instance recovers the original plaintext byte-for-byte. Crypto-correctness
    /// against published KAT vectors is covered separately by the KAT pipeline; this test pins the symmetric
    /// encrypt-then-decrypt contract using non-KAT incrementing input so it runs for every cipher variant
    /// regardless of whether published reference vectors exist.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void EncryptThenDecrypt_ShouldRoundTripCorrectly(TVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);

        byte[] plaintext = BuildArbitraryBlock(spec.BlockSize, offset: 0x80);
        byte[] ciphertext = new byte[spec.BlockSize];
        byte[] recovered = new byte[spec.BlockSize];

        cipher.Encrypt(plaintext, ciphertext);
        cipher.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting the same plaintext block twice in succession against the same cipher instance
    /// produces identical ciphertext — confirming the engine is deterministic and stateless between calls.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Encrypt_WhenCalledMultipleTimesWithSameInput_ShouldProduceSameOutput(TVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);

        byte[] plaintext = BuildArbitraryBlock(spec.BlockSize, offset: 0x10);
        byte[] first = new byte[spec.BlockSize];
        byte[] second = new byte[spec.BlockSize];

        cipher.Encrypt(plaintext, first);
        cipher.Encrypt(plaintext, second);

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that the ciphertext produced by <c>Encrypt</c> is not equal to its plaintext input — guarding
    /// against a no-op or identity-transform regression at the cipher tier.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Encrypt_ShouldTransformBlock(TVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);

        byte[] plaintext = BuildArbitraryBlock(spec.BlockSize, offset: 0xA0);
        byte[] ciphertext = new byte[spec.BlockSize];

        cipher.Encrypt(plaintext, ciphertext);

        CollectionAssert.AreNotEqual(plaintext, ciphertext);
    }

    /// <summary>
    /// Builds a deterministic block of <paramref name="length" /> bytes starting from
    /// <paramref name="offset" /> and incrementing with modular byte wrap-around, so the helper works for
    /// every block size in the suite (including the 1024-bit wide-block constructions where
    /// <paramref name="length" /> exceeds 256).
    /// </summary>
    private static byte[] BuildArbitraryBlock(int length, int offset)
    {
        byte[] buffer = new byte[length];
        for (int i = 0; i < length; i++)
            buffer[i] = (byte)(offset + i);
        return buffer;
    }
}
