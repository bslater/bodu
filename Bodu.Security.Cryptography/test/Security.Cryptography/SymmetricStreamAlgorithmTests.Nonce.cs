// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.Nonce.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Nonce" /> on a fresh cipher lazily generates a nonce of
    /// the required length.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenReadOnFreshCipher_ShouldGenerateNonceOfRequiredLength()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.AreEqual(NonceLengthBytes, cipher.Nonce.Length);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Nonce" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.Nonce);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to <see cref="SymmetricStreamAlgorithm.Nonce" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenSetToNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cipher.Nonce = null!;
        });
    }

    /// <summary>
    /// Verifies that assigning a nonce whose length in bytes is not <see cref="SymmetricStreamAlgorithm.NonceSize" />
    /// throws <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="nonceSizeBytes">The candidate nonce length, in bytes, expected to be rejected.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidNonceSizeBytesData))]
    public void Nonce_WhenSetToInvalidLength_ShouldThrowCryptographicException(int nonceSizeBytes)
    {
        if (nonceSizeBytes < 0) return;

        using TAlgorithm cipher = CreateAlgorithm();
        var invalidNonce = new byte[nonceSizeBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.Nonce = invalidNonce;
        });
    }

    /// <summary>
    /// Verifies that a nonce assigned to <see cref="SymmetricStreamAlgorithm.Nonce" /> round-trips on the subsequent
    /// get.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenSet_ShouldReturnSameValueOnGet()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        var nonce = new byte[NonceLengthBytes];
        CryptographyHelper.FillWithRandomNonZeroBytes(nonce);

        cipher.Nonce = nonce;

        CollectionAssert.AreEqual(nonce, cipher.Nonce);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.Nonce" /> returns a defensive copy rather than the supplied
    /// reference.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenSet_ShouldReturnDefensiveCopy()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        var nonce = new byte[NonceLengthBytes];
        CryptographyHelper.FillWithRandomNonZeroBytes(nonce);

        cipher.Nonce = nonce;

        Assert.AreNotSame(nonce, cipher.Nonce);
    }

    /// <summary>
    /// Verifies that the <see cref="SymmetricStreamAlgorithm.Nonce" /> getter returns an independent copy, so mutating
    /// the returned array does not change the cipher's stored nonce.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenMutatedAfterRead_ShouldNotAffectStoredNonce()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateNonce();

        var first = cipher.Nonce;
        first[0] ^= 0xFF;
        var second = cipher.Nonce;

        Assert.AreNotEqual(first[0], second[0]);
    }
}
