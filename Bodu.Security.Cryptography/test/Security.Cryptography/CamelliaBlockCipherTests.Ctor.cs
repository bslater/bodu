// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipherTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

internal sealed partial class CamelliaBlockCipherTests
{
    /// <summary>
    /// Verifies that constructing <see cref="CamelliaBlockCipher" /> with a key length that is not 16, 24, or 32
    /// bytes throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(23)]
    [DataRow(25)]
    [DataRow(31)]
    [DataRow(33)]
    public void Ctor_WhenKeyLengthIsInvalid_ShouldThrowExactly(int keyLength)
    {
        var key = new byte[keyLength];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using var _ = new CamelliaBlockCipher(key);
        });
    }

    /// <summary>
    /// Verifies that constructing <see cref="CamelliaBlockCipher" /> with each valid key length (16, 24, and 32
    /// bytes) succeeds without throwing.
    /// </summary>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void Ctor_WhenKeyLengthIsValid_ShouldNotThrow(int keyLength)
    {
        var key = new byte[keyLength];

        using var cipher = new CamelliaBlockCipher(key);
        Assert.IsNotNull(cipher);
    }

    /// <summary>
    /// Verifies that encrypting a known plaintext block and then decrypting the result with the same 128-bit key
    /// recovers the original plaintext exactly.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithKey128_ShouldRoundTripCorrectly()
    {
        var key = TestHelpers.GenerateIncrementalByteSequence(0, 16);
        var plaintext = TestHelpers.GenerateIncrementalByteSequence(0x80, 16);
        var ciphertext = new byte[16];
        var recovered = new byte[16];

        using var cipher = new CamelliaBlockCipher(key);
        cipher.Encrypt(plaintext, ciphertext);
        cipher.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting a known plaintext block and then decrypting the result with the same 192-bit key
    /// recovers the original plaintext exactly.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithKey192_ShouldRoundTripCorrectly()
    {
        var key = TestHelpers.GenerateIncrementalByteSequence(0, 24);
        var plaintext = TestHelpers.GenerateIncrementalByteSequence(0x80, 16);
        var ciphertext = new byte[16];
        var recovered = new byte[16];

        using var cipher = new CamelliaBlockCipher(key);
        cipher.Encrypt(plaintext, ciphertext);
        cipher.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting a known plaintext block and then decrypting the result with the same 256-bit key
    /// recovers the original plaintext exactly.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithKey256_ShouldRoundTripCorrectly()
    {
        var key = TestHelpers.GenerateIncrementalByteSequence(0, 32);
        var plaintext = TestHelpers.GenerateIncrementalByteSequence(0x80, 16);
        var ciphertext = new byte[16];
        var recovered = new byte[16];

        using var cipher = new CamelliaBlockCipher(key);
        cipher.Encrypt(plaintext, ciphertext);
        cipher.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting the same 128-bit block multiple times with the same key produces identical output,
    /// confirming the cipher is deterministic and stateless between calls.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCalledMultipleTimesWithSameInput_ShouldProduceSameOutput()
    {
        var key = TestHelpers.GenerateIncrementalByteSequence(0, 16);
        var plaintext = TestHelpers.GenerateIncrementalByteSequence(0x10, 16);
        var ct1 = new byte[16];
        var ct2 = new byte[16];

        using var cipher = new CamelliaBlockCipher(key);
        cipher.Encrypt(plaintext, ct1);
        cipher.Encrypt(plaintext, ct2);

        CollectionAssert.AreEqual(ct1, ct2);
    }

    /// <summary>
    /// Verifies that the ciphertext produced by encryption is not equal to the plaintext (i.e. that the cipher
    /// actually transforms the data), for all three key sizes.
    /// </summary>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void Encrypt_WhenKeyIsValid_ShouldTransformBlock(int keyLength)
    {
        var key = TestHelpers.GenerateIncrementalByteSequence(0, keyLength);
        var plaintext = TestHelpers.GenerateIncrementalByteSequence(0xA0, 16);
        var ciphertext = new byte[16];

        using var cipher = new CamelliaBlockCipher(key);
        cipher.Encrypt(plaintext, ciphertext);

        CollectionAssert.AreNotEqual(plaintext, ciphertext);
    }
}
