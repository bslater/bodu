// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmContractTests.Sizes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

public abstract partial class SymmetricStreamAlgorithmContractTests<TCipher>
{
    /// <summary>
    /// Verifies that a default-constructed cipher exposes the expected key size and nonce size, locking in the public
    /// algorithm shape.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCreated_ShouldExposeExpectedKeyAndNonceSizes()
    {
        using TCipher cipher = CreateAlgorithm();

        Assert.AreEqual(ExpectedKeySizeBits, cipher.KeySize);
        Assert.AreEqual(ExpectedNonceSizeBits, cipher.NonceSize);
    }

    /// <summary>
    /// Verifies that the cipher's default <see cref="SymmetricStreamAlgorithm.KeySize" /> is reported as one of its
    /// <see cref="SymmetricStreamAlgorithm.LegalKeySizes" />.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenQueried_ShouldContainDefaultKeySize()
    {
        using TCipher cipher = CreateAlgorithm();
        KeySizes[] legal = cipher.LegalKeySizes;

        bool covered = false;
        foreach (KeySizes size in legal)
        {
            if (cipher.KeySize < size.MinSize || cipher.KeySize > size.MaxSize)
                continue;

            covered = size.SkipSize == 0
                ? cipher.KeySize == size.MinSize
                : (cipher.KeySize - size.MinSize) % size.SkipSize == 0;

            if (covered)
                break;
        }

        Assert.IsTrue(covered, "The default key size should be one of the legal key sizes.");
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.LegalKeySizes" /> returns an independent copy, so mutating the
    /// returned array does not affect the cipher.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenQueriedTwice_ShouldReturnIndependentArrays()
    {
        using TCipher cipher = CreateAlgorithm();

        Assert.AreNotSame(cipher.LegalKeySizes, cipher.LegalKeySizes);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Key" /> on a fresh cipher lazily generates a key of the
    /// required length.
    /// </summary>
    [TestMethod]
    public void Key_WhenReadOnFreshCipher_ShouldGenerateKeyOfRequiredLength()
    {
        using TCipher cipher = CreateAlgorithm();

        Assert.AreEqual(KeyLengthBytes, cipher.Key.Length);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Nonce" /> on a fresh cipher lazily generates a nonce of
    /// the required length.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenReadOnFreshCipher_ShouldGenerateNonceOfRequiredLength()
    {
        using TCipher cipher = CreateAlgorithm();

        Assert.AreEqual(NonceLengthBytes, cipher.Nonce.Length);
    }

    /// <summary>
    /// Verifies that the <see cref="SymmetricStreamAlgorithm.Key" /> getter returns an independent copy, so mutating the
    /// returned array does not change the cipher's stored key.
    /// </summary>
    [TestMethod]
    public void Key_WhenMutatedAfterRead_ShouldNotAffectStoredKey()
    {
        using TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();

        byte[] first = cipher.Key;
        first[0] ^= 0xFF;
        byte[] second = cipher.Key;

        Assert.AreNotEqual(first[0], second[0]);
    }

    /// <summary>
    /// Verifies that assigning a nonce whose length is not <see cref="SymmetricStreamAlgorithm.NonceSize" /> throws a
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenSetToInvalidLength_ShouldThrowCryptographicException()
    {
        using TCipher cipher = CreateAlgorithm();
        byte[] wrongNonce = new byte[NonceLengthBytes + 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.Nonce = wrongNonce;
        });
    }

    /// <summary>
    /// Verifies that assigning a key whose length is not one of the cipher's legal key sizes throws a
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToInvalidLength_ShouldThrowCryptographicException()
    {
        using TCipher cipher = CreateAlgorithm();
        byte[] shortKey = new byte[KeyLengthBytes - 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.Key = shortKey;
        });
    }
}
