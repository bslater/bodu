// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipherTests.Rekey.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class ThreefishBlockCipherTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // ThreefishBlockCipher.Rekey(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ThreefishBlockCipher.Rekey" /> throws <see cref="ArgumentException" />
    /// when the supplied key is shorter than the required key size.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenKeyIsTooShort_ShouldThrowExactly()
    {
        using var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Rekey(new byte[31], s_validTweak);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ThreefishBlockCipher.Rekey" /> throws <see cref="ArgumentException" />
    /// when the supplied key is longer than the required key size.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenKeyIsTooLong_ShouldThrowExactly()
    {
        using var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Rekey(new byte[33], s_validTweak);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ThreefishBlockCipher.Rekey" /> throws <see cref="ArgumentException" />
    /// when supplied an empty key.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenKeyIsEmpty_ShouldThrowExactly()
    {
        using var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Rekey(ReadOnlySpan<byte>.Empty, s_validTweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThreefishBlockCipher.Rekey" /> throws <see cref="ArgumentException" />
    /// when the supplied tweak is shorter than the required 16-byte tweak size.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenTweakIsTooShort_ShouldThrowExactly()
    {
        using var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Rekey(s_validKey256, new byte[15]);
        });

        Assert.AreEqual("tweak", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ThreefishBlockCipher.Rekey" /> throws <see cref="ArgumentException" />
    /// when the supplied tweak is longer than the required 16-byte tweak size.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenTweakIsTooLong_ShouldThrowExactly()
    {
        using var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Rekey(s_validKey256, new byte[17]);
        });

        Assert.AreEqual("tweak", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ThreefishBlockCipher.Rekey" /> throws <see cref="ArgumentException" />
    /// when supplied an empty tweak.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenTweakIsEmpty_ShouldThrowExactly()
    {
        using var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Rekey(s_validKey256, ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThreefishBlockCipher.Rekey" /> throws <see cref="ObjectDisposedException" />
    /// when called on a disposed cipher instance.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenDisposed_ShouldThrowExactly()
    {
        var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.Rekey(s_validKey256, s_validTweak);
        });
    }

    /// <summary>
    /// Verifies that a successful <see cref="ThreefishBlockCipher.Rekey" /> call changes the encryption
    /// output, confirming that the new key and tweak are actually applied.
    /// </summary>
    [TestMethod]
    public void Rekey_WhenValidKeyAndTweakProvided_ShouldChangeEncryptionOutput()
    {
        using var cipher = new Threefish256Cipher(s_validKey256, s_validTweak);

        var plaintext = new byte[32];
        var outputBefore = new byte[32];
        var outputAfter = new byte[32];

        cipher.Encrypt(plaintext, outputBefore);

        var newKey = new byte[32];
        newKey[0] = 0x01;
        cipher.Rekey(newKey, s_validTweak);

        cipher.Encrypt(plaintext, outputAfter);

        CollectionAssert.AreNotEqual(outputBefore, outputAfter,
            "Rekey with a different key must produce different ciphertext.");
    }
}
