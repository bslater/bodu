// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class GcmSivModeTransformTests
{
    /// <summary>
    /// Verifies that naming a 256-bit key-generating key produces RFC 8452's AES-256-GCM-SIV output through a master
    /// cipher whose key size the transform cannot read for itself.
    /// </summary>
    /// <param name="vector">The AES-256-GCM-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(GcmSivRfc8452Aes256Vectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Ctor_WhenKeySizeIs256_ForAnyBlockCipher_ShouldMatchRfc8452Vector(AeadKnownAnswer vector)
    {
        byte[] iv = new byte[16];
        vector.Nonce.CopyTo(iv, 0);

        using var transform = new GcmSivModeTransform(
            new AesBlockCipherFixture(vector.Key!), k => new AesBlockCipherFixture(k), iv, keySize: 256);
        if (vector.AssociatedData.Length > 0) transform.ProcessAssociatedData(vector.AssociatedData);
        byte[] output = new byte[vector.Plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(vector.Plaintext, output);

        CollectionAssert.AreEqual(vector.CiphertextWithTag, output, $"GCM-SIV encrypt mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that a key size RFC 8452 does not define is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> naming the parameter.
    /// </summary>
    /// <param name="keySize">The rejected key size, in bits.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-128)]
    [DataRow(64)]
    [DataRow(192)]
    [DataRow(512)]
    public void Ctor_WhenKeySizeIsNot128Or256_ShouldThrowArgumentOutOfRangeException(int keySize)
    {
        using var master = new AesBlockCipherFixture(new byte[16]);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new GcmSivModeTransform(master, k => new AesBlockCipherFixture(k), new byte[16], keySize);
        });

        Assert.AreEqual("keySize", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the overload without a key size hands the cipher factory a message-encryption key as long as an
    /// <see cref="AesBlockCipher" /> master key of 128 or 256 bits, and a 128-bit one for AES-192, which RFC 8452 does
    /// not define.
    /// </summary>
    /// <param name="masterKeyBytes">The length, in bytes, of the <see cref="AesBlockCipher" /> master key.</param>
    /// <param name="expectedEncryptionKeyBytes">The length, in bytes, the factory is expected to receive.</param>
    [TestMethod]
    [DataRow(16, 16)]
    [DataRow(24, 16)]
    [DataRow(32, 32)]
    public void Ctor_WhenMasterCipherIsAesBlockCipher_ShouldDeriveEncryptionKeyOfMatchingSize(int masterKeyBytes, int expectedEncryptionKeyBytes)
    {
        int received = -1;
        using var master = new AesBlockCipher(new byte[masterKeyBytes]);

        using var transform = new GcmSivModeTransform(master, k => { received = k.Length; return new AesBlockCipher(k); }, new byte[16]);

        Assert.AreEqual(expectedEncryptionKeyBytes, received);
    }

    /// <summary>
    /// Verifies that the overload without a key size keeps a 128-bit message-encryption key for a master cipher whose
    /// key size it cannot read, whatever that cipher's key length.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMasterCipherKeySizeIsUnknown_ShouldDeriveA128BitEncryptionKey()
    {
        int received = -1;
        using var master = new AesBlockCipherFixture(new byte[32]);

        using var transform = new GcmSivModeTransform(master, k => { received = k.Length; return new AesBlockCipherFixture(k); }, new byte[16]);

        Assert.AreEqual(16, received);
    }
}
