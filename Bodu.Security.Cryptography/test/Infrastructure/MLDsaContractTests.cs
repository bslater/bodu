// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Provides the shared behavioral contract tests for every <see cref="MLDsa" /> parameter set: construction
/// defaults, key lifecycle, sign/verify round-trips, context handling, tamper rejection, argument validation, and
/// disposal.
/// </summary>
/// <typeparam name="TDsa">The concrete parameter-set type under test.</typeparam>
public abstract class MLDsaContractTests<TDsa>
    where TDsa : MLDsa, new()
{
    /// <summary>
    /// Gets the parameter-set designator reported through <see cref="AsymmetricAlgorithm.KeySize" />.
    /// </summary>
    protected abstract int ExpectedKeySizeDesignator { get; }

    /// <summary>
    /// Gets the expected public key size in bytes.
    /// </summary>
    protected abstract int ExpectedPublicKeySize { get; }

    /// <summary>
    /// Gets the expected private key size in bytes.
    /// </summary>
    protected abstract int ExpectedPrivateKeySize { get; }

    /// <summary>
    /// Gets the expected signature size in bytes.
    /// </summary>
    protected abstract int ExpectedSignatureSize { get; }

    /// <summary>
    /// Verifies that a newly constructed instance reports the parameter-set designator, the documented encoding
    /// sizes, hedged signing, and no key material.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldReportParameterSetSizesAndNoKeys()
    {
        using var dsa = new TDsa();

        Assert.AreEqual(ExpectedKeySizeDesignator, dsa.KeySize);
        Assert.AreEqual(ExpectedPublicKeySize, dsa.PublicKeySizeInBytes);
        Assert.AreEqual(ExpectedPrivateKeySize, dsa.PrivateKeySizeInBytes);
        Assert.AreEqual(ExpectedSignatureSize, dsa.SignatureSizeInBytes);
        Assert.IsFalse(dsa.HasPrivateKey);
        Assert.IsFalse(dsa.HasPublicKey);
        Assert.IsFalse(dsa.DeterministicSigning);
        Assert.AreEqual("ML-DSA", dsa.SignatureAlgorithm);
        Assert.IsNull(dsa.KeyExchangeAlgorithm);
    }

    /// <summary>
    /// Verifies that a signature produced over a fresh key pair verifies, including through a verify-only instance.
    /// </summary>
    [TestMethod]
    public void SignData_WhenVerifiedWithMatchingKey_ShouldReturnTrue()
    {
        using var signer = new TDsa();
        signer.GenerateKey();
        var message = new byte[] { 1, 2, 3, 4, 5 };

        var signature = signer.SignData(message);
        Assert.AreEqual(ExpectedSignatureSize, signature.Length);
        Assert.IsTrue(signer.VerifyData(message, signature));

        using var verifier = new TDsa();
        verifier.ImportPublicKey(signer.ExportPublicKey());
        Assert.IsTrue(verifier.VerifyData(message, signature));
    }

    /// <summary>
    /// Verifies that hedged signing produces differing signatures across calls while deterministic signing
    /// reproduces the identical signature, and that both verify.
    /// </summary>
    [TestMethod]
    public void SignData_WhenHedgedVersusDeterministic_ShouldDifferAndReproduceRespectively()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        var message = new byte[] { 42 };

        var hedgedFirst = dsa.SignData(message);
        var hedgedSecond = dsa.SignData(message);
        CollectionAssert.AreNotEqual(hedgedFirst, hedgedSecond);
        Assert.IsTrue(dsa.VerifyData(message, hedgedFirst));
        Assert.IsTrue(dsa.VerifyData(message, hedgedSecond));

        dsa.DeterministicSigning = true;
        CollectionAssert.AreEqual(dsa.SignData(message), dsa.SignData(message));
    }

    /// <summary>
    /// Verifies that a signature bound to a context string verifies only under the same context.
    /// </summary>
    [TestMethod]
    public void SignData_WhenContextIsBound_ShouldVerifyOnlyUnderSameContext()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        var message = new byte[] { 7, 7, 7 };
        var context = new byte[] { 0xC0, 0xFF, 0xEE };

        var signature = dsa.SignData(message, context);

        Assert.IsTrue(dsa.VerifyData(message, signature, context));
        Assert.IsFalse(dsa.VerifyData(message, signature));
        Assert.IsFalse(dsa.VerifyData(message, signature, new byte[] { 0xC0, 0xFF, 0xEF }));
    }

    /// <summary>
    /// Verifies that tampering with the message or any signature region causes verification to return
    /// <see langword="false" />, and that wrong-length signatures return <see langword="false" /> without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSignatureOrMessageIsTampered_ShouldReturnFalse()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        var message = new byte[] { 9, 8, 7, 6 };
        var signature = dsa.SignData(message);

        var tamperedCommitment = (byte[])signature.Clone();
        tamperedCommitment[0] ^= 0x01;
        Assert.IsFalse(dsa.VerifyData(message, tamperedCommitment));

        var tamperedZ = (byte[])signature.Clone();
        tamperedZ[ExpectedSignatureSize / 2] ^= 0x01;
        Assert.IsFalse(dsa.VerifyData(message, tamperedZ));

        var tamperedMessage = (byte[])message.Clone();
        tamperedMessage[1] ^= 0x01;
        Assert.IsFalse(dsa.VerifyData(tamperedMessage, signature));

        Assert.IsFalse(dsa.VerifyData(message, signature.AsSpan(0, ExpectedSignatureSize - 1)));
        Assert.IsFalse(dsa.VerifyData(message, ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Verifies that the exported private key round-trips through <see cref="MLDsa.ImportPrivateKey" />, restoring
    /// both keys, and that the same seed regenerates the same key pair.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenRoundTrippingExportedKey_ShouldRestoreSigningAndPublicKey()
    {
        using var original = new TDsa();
        original.GenerateKey();
        var message = new byte[] { 5, 5, 5 };

        using var restored = new TDsa();
        restored.ImportPrivateKey(original.ExportPrivateKey());

        Assert.IsTrue(restored.HasPublicKey);
        CollectionAssert.AreEqual(original.ExportPublicKey(), restored.ExportPublicKey());

        restored.DeterministicSigning = true;
        Assert.IsTrue(original.VerifyData(message, restored.SignData(message)));

        var seed = new byte[MLDsa.PrivateSeedSizeInBytes];
        new Random(204).NextBytes(seed);
        using var first = new TDsa();
        using var second = new TDsa();
        first.ImportPrivateSeed(seed);
        second.ImportPrivateSeed(seed);
        CollectionAssert.AreEqual(first.ExportPrivateKey(), second.ExportPrivateKey());
    }

    /// <summary>
    /// Verifies that <see cref="MLDsa.ImportPrivateKey" /> rejects a private key whose embedded public-key hash is
    /// inconsistent with its secret vectors.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenEmbeddedHashIsCorrupted_ShouldThrowArgumentException()
    {
        using var donor = new TDsa();
        donor.GenerateKey();

        var corrupted = donor.ExportPrivateKey();
        corrupted[70] ^= 0x01;

        using var dsa = new TDsa();
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dsa.ImportPrivateKey(corrupted);
        });

        Assert.AreEqual("privateKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that wrong-length keys, oversized contexts, and wrong-length destinations throw
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Members_WhenGivenInvalidArguments_ShouldThrowArgumentException()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();

        Assert.ThrowsExactly<ArgumentException>(() => { dsa.ImportPrivateSeed(new byte[31]); });
        Assert.ThrowsExactly<ArgumentException>(() => { dsa.ImportPublicKey(new byte[ExpectedPublicKeySize + 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { dsa.ImportPrivateKey(new byte[ExpectedPrivateKeySize - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { _ = dsa.SignData(new byte[1], new byte[256]); });
        Assert.ThrowsExactly<ArgumentException>(() => { dsa.SignData(new byte[1], ReadOnlySpan<byte>.Empty, new byte[ExpectedSignatureSize - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { _ = dsa.VerifyData(new byte[1], new byte[ExpectedSignatureSize], new byte[256]); });
    }

    /// <summary>
    /// Verifies that operations requiring missing key material throw <see cref="CryptographicException" />, and
    /// that a verify-only instance cannot sign.
    /// </summary>
    [TestMethod]
    public void Members_WhenRequiredKeyMaterialIsMissing_ShouldThrowCryptographicException()
    {
        using var empty = new TDsa();
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.SignData(new byte[1]); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.VerifyData(new byte[1], new byte[ExpectedSignatureSize]); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.ExportPrivateKey(); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.ExportPublicKey(); });

        using var donor = new TDsa();
        donor.GenerateKey();

        using var verifyOnly = new TDsa();
        verifyOnly.ImportPublicKey(donor.ExportPublicKey());
        Assert.IsFalse(verifyOnly.HasPrivateKey);
        Assert.ThrowsExactly<CryptographicException>(() => { _ = verifyOnly.SignData(new byte[1]); });
    }

    /// <summary>
    /// Verifies that members throw <see cref="ObjectDisposedException" /> after disposal and that double disposal
    /// is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeSubsequentMemberAccessThrowObjectDisposedException()
    {
        var dsa = new TDsa();
        dsa.GenerateKey();
        var message = new byte[] { 1 };
        var signature = dsa.SignData(message);

        dsa.Dispose();
        dsa.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = dsa.HasPrivateKey; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { dsa.GenerateKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = dsa.SignData(message); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = dsa.VerifyData(message, signature); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = dsa.ExportPublicKey(); });
    }
}
