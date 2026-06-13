// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for digital signature algorithms, extending
/// <see cref="AsymmetricAlgorithmTests{TTest, TAlgorithm}" /> with the sign/verify contract: round-trips including
/// the empty message, verify-only instances, tamper and malformed-signature rejection, missing-key failures, and
/// disposal of the signing members.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TAlgorithm">The type of signature algorithm under test.</typeparam>
[TestClass]
public abstract class SignatureAlgorithmTests<TTest, TAlgorithm>
    : AsymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : SignatureAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : AsymmetricAlgorithm, new()
{
    /// <summary>
    /// Gets the exact size, in bytes, of an encoded signature.
    /// </summary>
    protected abstract int SignatureSizeBytes { get; }

    /// <summary>
    /// Signs the supplied data with the instance's private key.
    /// </summary>
    /// <param name="algorithm">The instance holding the private key.</param>
    /// <param name="data">The message bytes to sign.</param>
    /// <returns>The encoded signature.</returns>
    protected abstract byte[] SignData(TAlgorithm algorithm, byte[] data);

    /// <summary>
    /// Verifies a candidate signature over the supplied data against the instance's public key.
    /// </summary>
    /// <param name="algorithm">The instance holding the public key.</param>
    /// <param name="data">The message bytes that were signed.</param>
    /// <param name="signature">The candidate signature.</param>
    /// <returns><see langword="true" /> when the signature is valid.</returns>
    protected abstract bool VerifyData(TAlgorithm algorithm, byte[] data, byte[] signature);

    /// <summary>
    /// Verifies that a signature of the documented size round-trips through verification, including the empty
    /// message and through a verify-only instance.
    /// </summary>
    [TestMethod]
    public void SignData_WhenVerifiedWithMatchingKey_ShouldReturnTrue()
    {
        using TAlgorithm signer = CreateAlgorithmWithGeneratedKey();
        var message = new byte[] { 1, 2, 3, 4, 5 };

        var signature = SignData(signer, message);
        Assert.AreEqual(SignatureSizeBytes, signature.Length);
        Assert.IsTrue(VerifyData(signer, message, signature));

        var emptySignature = SignData(signer, Array.Empty<byte>());
        Assert.IsTrue(VerifyData(signer, Array.Empty<byte>(), emptySignature));

        using TAlgorithm verifier = CreatePublicOnlyAlgorithm(signer);
        Assert.IsTrue(VerifyData(verifier, message, signature));
    }

    /// <summary>
    /// Verifies that flipping a bit at the start, middle, or end of the signature — or in the message — causes
    /// verification to return <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSignatureOrMessageIsTampered_ShouldReturnFalse()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var message = new byte[] { 9, 8, 7, 6 };
        var signature = SignData(algorithm, message);

        foreach (var index in new[] { 0, SignatureSizeBytes / 2, SignatureSizeBytes - 1 })
        {
            var tampered = (byte[])signature.Clone();
            tampered[index] ^= 0x01;

            Assert.IsFalse(VerifyData(algorithm, message, tampered), $"A signature tampered at byte {index} must not verify.");
        }

        var tamperedMessage = (byte[])message.Clone();
        tamperedMessage[1] ^= 0x01;
        Assert.IsFalse(VerifyData(algorithm, tamperedMessage, signature));
    }

    /// <summary>
    /// Verifies that wrong-length signatures return <see langword="false" /> without throwing.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSignatureLengthIsInvalid_ShouldReturnFalse()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var message = new byte[] { 1 };

        foreach (var length in new[] { 0, SignatureSizeBytes - 1, SignatureSizeBytes + 1 })
            Assert.IsFalse(VerifyData(algorithm, message, new byte[length]), $"A {length}-byte signature must not verify.");
    }

    /// <summary>
    /// Verifies that signing without a private key — and verifying without a public key — throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void SignAndVerify_WhenRequiredKeyMaterialIsMissing_ShouldThrowCryptographicException()
    {
        var message = new byte[] { 1 };

        using TAlgorithm empty = CreateAlgorithm();
        Assert.ThrowsExactly<CryptographicException>(() => { _ = SignData(empty, message); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = VerifyData(empty, message, new byte[SignatureSizeBytes]); });

        using TAlgorithm donor = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm verifyOnly = CreatePublicOnlyAlgorithm(donor);
        Assert.ThrowsExactly<CryptographicException>(() => { _ = SignData(verifyOnly, message); });
    }

    /// <summary>
    /// Verifies that the signing members throw <see cref="ObjectDisposedException" /> after the instance has been
    /// disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeSignAndVerifyThrowObjectDisposedException()
    {
        TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var message = new byte[] { 1 };
        var signature = SignData(algorithm, message);

        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = SignData(algorithm, message); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = VerifyData(algorithm, message, signature); });
    }
}
