// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyAgreementAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for key-agreement algorithms, extending
/// <see cref="AsymmetricAlgorithmTests{TTest, TAlgorithm}" /> with the shared-secret derivation contract: mutual
/// agreement, determinism, missing-key failures, and disposal of the derivation member.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TAlgorithm">The type of key-agreement algorithm under test.</typeparam>
[TestClass]
public abstract class KeyAgreementAlgorithmTests<TTest, TAlgorithm>
    : AsymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : KeyAgreementAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : AsymmetricAlgorithm, new()
{
    /// <summary>
    /// Gets the exact size, in bytes, of the derived shared secret.
    /// </summary>
    protected abstract int SharedSecretSizeBytes { get; }

    /// <summary>
    /// Derives the shared secret between the instance's private key and a peer public key.
    /// </summary>
    /// <param name="algorithm">The instance holding the local private key.</param>
    /// <param name="peerPublicKey">The peer's raw public key.</param>
    /// <returns>The derived shared secret.</returns>
    protected abstract byte[] DeriveSharedSecret(TAlgorithm algorithm, byte[] peerPublicKey);

    /// <summary>
    /// Verifies that two freshly generated parties derive the same shared secret of the documented size from each
    /// other's public keys.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenBothPartiesExchangeKeys_ShouldAgree()
    {
        using TAlgorithm alice = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm bob = CreateAlgorithmWithGeneratedKey();

        var aliceShared = DeriveSharedSecret(alice, ExportPublicKey(bob));
        var bobShared = DeriveSharedSecret(bob, ExportPublicKey(alice));

        Assert.AreEqual(SharedSecretSizeBytes, aliceShared.Length);
        CollectionAssert.AreEqual(aliceShared, bobShared);
    }

    /// <summary>
    /// Verifies that the derivation is deterministic: deriving twice against the same peer yields the same secret.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenRepeatedWithSamePeer_ShouldYieldSameSecret()
    {
        using TAlgorithm local = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm peer = CreateAlgorithmWithGeneratedKey();
        var peerPublic = ExportPublicKey(peer);

        CollectionAssert.AreEqual(DeriveSharedSecret(local, peerPublic), DeriveSharedSecret(local, peerPublic));
    }

    /// <summary>
    /// Verifies that deriving without a private key throws <see cref="CryptographicException" />, including on a
    /// public-only instance.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenNoPrivateKeyPresent_ShouldThrowCryptographicException()
    {
        using TAlgorithm donor = CreateAlgorithmWithGeneratedKey();
        var peerPublic = ExportPublicKey(donor);

        using TAlgorithm empty = CreateAlgorithm();
        Assert.ThrowsExactly<CryptographicException>(() => { _ = DeriveSharedSecret(empty, peerPublic); });

        using TAlgorithm publicOnly = CreatePublicOnlyAlgorithm(donor);
        Assert.ThrowsExactly<CryptographicException>(() => { _ = DeriveSharedSecret(publicOnly, peerPublic); });
    }

    /// <summary>
    /// Verifies that deriving against a peer public key of the wrong length throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenPeerKeyLengthIsInvalid_ShouldThrowArgumentException()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        foreach (var length in new[] { 0, spec.PublicKeySizeBytes - 1, spec.PublicKeySizeBytes + 1 })
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => { _ = DeriveSharedSecret(algorithm, new byte[length]); },
                $"DeriveSharedSecret must reject a {length}-byte peer key.");
        }
    }

    /// <summary>
    /// Verifies that the derivation member throws <see cref="ObjectDisposedException" /> after the instance has
    /// been disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeDeriveSharedSecretThrowObjectDisposedException()
    {
        using TAlgorithm peer = CreateAlgorithmWithGeneratedKey();
        var peerPublic = ExportPublicKey(peer);

        TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = DeriveSharedSecret(algorithm, peerPublic); });
    }
}
