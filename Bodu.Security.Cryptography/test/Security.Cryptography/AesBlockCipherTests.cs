// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the public <see cref="AesBlockCipher" /> adapter: key-size validation, single-block
/// encrypt / decrypt round-trip against the BCL's <see cref="Aes" />, and disposal semantics.
/// </summary>
[TestClass]
public sealed class AesBlockCipherTests
{
    /// <summary>
    /// Verifies that constructing with a <see langword="null" /> key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new AesBlockCipher(null!));
    }

    /// <summary>
    /// Verifies that construction fails with <see cref="CryptographicException" /> when the supplied
    /// key length is not one of the three AES variants (128 / 192 / 256 bits).
    /// </summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(31)]
    [DataRow(33)]
    public void Ctor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException(int keyLength)
    {
        byte[] key = new byte[keyLength];
        Assert.ThrowsException<CryptographicException>(() => new AesBlockCipher(key));
    }

    /// <summary>
    /// Verifies that <see cref="AesBlockCipher.BlockSize" /> reports the fixed AES block size of
    /// 16 bytes regardless of key length.
    /// </summary>
    [DataTestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void BlockSize_ForAnyValidKeyLength_ShouldBe16(int keyLength)
    {
        using var cipher = new AesBlockCipher(new byte[keyLength]);
        Assert.AreEqual(16, cipher.BlockSize);
    }

    /// <summary>
    /// Verifies that a single block encrypted by <see cref="AesBlockCipher" /> is recovered
    /// byte-for-byte by a subsequent <see cref="AesBlockCipher.Decrypt" /> call using the same key.
    /// </summary>
    [DataTestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void EncryptThenDecrypt_ShouldRecoverOriginalPlaintextBlock(int keyLength)
    {
        byte[] key = new byte[keyLength];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = new byte[16];
        RandomNumberGenerator.Fill(plaintext);

        byte[] ciphertext = new byte[16];
        byte[] recovered = new byte[16];

        using (var cipher = new AesBlockCipher(key))
            cipher.Encrypt(plaintext, ciphertext);

        using (var cipher = new AesBlockCipher(key))
            cipher.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <see cref="AesBlockCipher" /> produces byte-for-byte identical output to the
    /// BCL's <see cref="Aes.EncryptEcb(ReadOnlySpan{byte}, Span{byte}, PaddingMode)" /> — confirming
    /// the adapter applies no additional transformation.
    /// </summary>
    [TestMethod]
    public void Encrypt_ForSingleBlock_ShouldMatchBclEncryptEcb()
    {
        byte[] key = RandomNumberGenerator.GetBytes(16);
        byte[] plaintext = RandomNumberGenerator.GetBytes(16);

        byte[] ourCiphertext = new byte[16];
        using (var cipher = new AesBlockCipher(key))
            cipher.Encrypt(plaintext, ourCiphertext);

        byte[] bclCiphertext = new byte[16];
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.EncryptEcb(plaintext, bclCiphertext, PaddingMode.None);
        }

        CollectionAssert.AreEqual(bclCiphertext, ourCiphertext);
    }

    /// <summary>
    /// Verifies that calling <see cref="AesBlockCipher.Encrypt" /> after <see cref="AesBlockCipher.Dispose" />
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var cipher = new AesBlockCipher(new byte[16]);
        cipher.Dispose();

        byte[] input = new byte[16], output = new byte[16];
        Assert.ThrowsException<ObjectDisposedException>(() => cipher.Encrypt(input, output));
    }

    /// <summary>
    /// Verifies that calling <see cref="AesBlockCipher.Decrypt" /> after <see cref="AesBlockCipher.Dispose" />
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var cipher = new AesBlockCipher(new byte[16]);
        cipher.Dispose();

        byte[] input = new byte[16], output = new byte[16];
        Assert.ThrowsException<ObjectDisposedException>(() => cipher.Decrypt(input, output));
    }

    /// <summary>
    /// Verifies that <see cref="AesBlockCipher.Dispose" /> is idempotent — calling it twice does not
    /// throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var cipher = new AesBlockCipher(new byte[16]);
        cipher.Dispose();
        cipher.Dispose();
    }
}
