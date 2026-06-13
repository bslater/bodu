// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KemAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for key-encapsulation mechanisms, extending
/// <see cref="AsymmetricAlgorithmTests{TTest, TAlgorithm}" /> with the encapsulate/decapsulate contract:
/// round-trips, implicit rejection of tampered ciphertexts, encapsulate-only instances, missing-key failures, and
/// disposal of the KEM members.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TAlgorithm">The type of key-encapsulation algorithm under test.</typeparam>
[TestClass]
public abstract class KemAlgorithmTests<TTest, TAlgorithm>
    : AsymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : KemAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : AsymmetricAlgorithm, new()
{
    /// <summary>
    /// Gets the exact size, in bytes, of the ciphertext produced by encapsulation.
    /// </summary>
    protected abstract int CiphertextSizeBytes { get; }

    /// <summary>
    /// Gets the exact size, in bytes, of the shared secret produced by encapsulation and decapsulation.
    /// </summary>
    protected abstract int SharedSecretSizeBytes { get; }

    /// <summary>
    /// Encapsulates a fresh shared secret to the instance's public (encapsulation) key.
    /// </summary>
    /// <param name="algorithm">The instance holding the encapsulation key.</param>
    /// <returns>The ciphertext and the shared secret.</returns>
    protected abstract (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(TAlgorithm algorithm);

    /// <summary>
    /// Recovers the shared secret from a ciphertext using the instance's private (decapsulation) key.
    /// </summary>
    /// <param name="algorithm">The instance holding the decapsulation key.</param>
    /// <param name="ciphertext">The candidate ciphertext.</param>
    /// <returns>The recovered shared secret.</returns>
    protected abstract byte[] Decapsulate(TAlgorithm algorithm, byte[] ciphertext);

    /// <summary>
    /// Verifies that encapsulating against a fresh key pair and decapsulating the ciphertext yields the same shared
    /// secret with the documented sizes, including through an encapsulate-only instance.
    /// </summary>
    [TestMethod]
    public void Encapsulate_WhenDecapsulatedWithMatchingKey_ShouldAgreeOnSharedSecret()
    {
        using TAlgorithm receiver = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm sender = CreatePublicOnlyAlgorithm(receiver);

        (var ciphertext, var senderSecret) = Encapsulate(sender);
        var receiverSecret = Decapsulate(receiver, ciphertext);

        Assert.AreEqual(CiphertextSizeBytes, ciphertext.Length);
        Assert.AreEqual(SharedSecretSizeBytes, senderSecret.Length);
        CollectionAssert.AreEqual(senderSecret, receiverSecret);
    }

    /// <summary>
    /// Verifies that a tampered ciphertext of the correct length decapsulates without throwing but yields a shared
    /// secret different from the encapsulating party's, and that the rejection is deterministic across repeated
    /// decapsulations of the same tampered ciphertext.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextIsTampered_ShouldImplicitlyRejectDeterministically()
    {
        using TAlgorithm receiver = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm sender = CreatePublicOnlyAlgorithm(receiver);
        (var ciphertext, var senderSecret) = Encapsulate(sender);

        ciphertext[0] ^= 0x01;
        var rejected = Decapsulate(receiver, ciphertext);

        Assert.AreEqual(SharedSecretSizeBytes, rejected.Length);
        CollectionAssert.AreNotEqual(senderSecret, rejected);
        CollectionAssert.AreEqual(rejected, Decapsulate(receiver, ciphertext));
    }

    /// <summary>
    /// Verifies that decapsulating a ciphertext of the wrong length throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextLengthIsInvalid_ShouldThrowArgumentException()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        foreach (var length in new[] { 0, CiphertextSizeBytes - 1, CiphertextSizeBytes + 1 })
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => { _ = Decapsulate(algorithm, new byte[length]); },
                $"Decapsulate must reject a {length}-byte ciphertext.");
        }
    }

    /// <summary>
    /// Verifies that encapsulating without a public key — and decapsulating without a private key, including on an
    /// encapsulate-only instance — throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void EncapsulateAndDecapsulate_WhenRequiredKeyMaterialIsMissing_ShouldThrowCryptographicException()
    {
        using TAlgorithm empty = CreateAlgorithm();
        Assert.ThrowsExactly<CryptographicException>(() => { _ = Encapsulate(empty); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = Decapsulate(empty, new byte[CiphertextSizeBytes]); });

        using TAlgorithm donor = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm encapsulateOnly = CreatePublicOnlyAlgorithm(donor);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = Decapsulate(encapsulateOnly, new byte[CiphertextSizeBytes]);
        });
    }

    /// <summary>
    /// Verifies that the KEM members throw <see cref="ObjectDisposedException" /> after the instance has been
    /// disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeEncapsulateAndDecapsulateThrowObjectDisposedException()
    {
        TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var ciphertext = new byte[CiphertextSizeBytes];

        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = Encapsulate(algorithm); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = Decapsulate(algorithm, ciphertext); });
    }
}
