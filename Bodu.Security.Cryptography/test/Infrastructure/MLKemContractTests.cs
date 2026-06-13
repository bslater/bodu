// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Provides the shared behavioral contract tests for every <see cref="MLKem" /> parameter set: construction
/// defaults, key lifecycle, encapsulate/decapsulate round-trips, implicit rejection, argument validation, and
/// disposal.
/// </summary>
/// <typeparam name="TKem">The concrete parameter-set type under test.</typeparam>
public abstract class MLKemContractTests<TKem>
    where TKem : MLKem, new()
{
    /// <summary>
    /// Gets the parameter-set designator reported through <see cref="AsymmetricAlgorithm.KeySize" />.
    /// </summary>
    protected abstract int ExpectedKeySizeDesignator { get; }

    /// <summary>
    /// Gets the expected encapsulation key size in bytes.
    /// </summary>
    protected abstract int ExpectedEncapsulationKeySize { get; }

    /// <summary>
    /// Gets the expected decapsulation key size in bytes.
    /// </summary>
    protected abstract int ExpectedDecapsulationKeySize { get; }

    /// <summary>
    /// Gets the expected ciphertext size in bytes.
    /// </summary>
    protected abstract int ExpectedCiphertextSize { get; }

    /// <summary>
    /// Verifies that a newly constructed instance reports the parameter-set designator, the documented encoding
    /// sizes, and no key material.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldReportParameterSetSizesAndNoKeys()
    {
        using var kem = new TKem();

        Assert.AreEqual(ExpectedKeySizeDesignator, kem.KeySize);
        Assert.AreEqual(ExpectedEncapsulationKeySize, kem.EncapsulationKeySizeInBytes);
        Assert.AreEqual(ExpectedDecapsulationKeySize, kem.DecapsulationKeySizeInBytes);
        Assert.AreEqual(ExpectedCiphertextSize, kem.CiphertextSizeInBytes);
        Assert.IsFalse(kem.HasDecapsulationKey);
        Assert.IsFalse(kem.HasEncapsulationKey);
        Assert.AreEqual("ML-KEM", kem.KeyExchangeAlgorithm);
        Assert.IsNull(kem.SignatureAlgorithm);
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.GenerateKey" /> populates both keys with the documented sizes.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldPopulateBothKeysWithDocumentedSizes()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        Assert.IsTrue(kem.HasDecapsulationKey);
        Assert.IsTrue(kem.HasEncapsulationKey);
        Assert.AreEqual(ExpectedEncapsulationKeySize, kem.ExportEncapsulationKey().Length);
        Assert.AreEqual(ExpectedDecapsulationKeySize, kem.ExportDecapsulationKey().Length);
    }

    /// <summary>
    /// Verifies that encapsulating against a fresh key pair and decapsulating the ciphertext yields the same
    /// 32-byte shared secret on both sides.
    /// </summary>
    [TestMethod]
    public void Encapsulate_WhenDecapsulatedWithMatchingKey_ShouldAgreeOnSharedSecret()
    {
        using var receiver = new TKem();
        receiver.GenerateKey();

        using var sender = new TKem();
        sender.ImportEncapsulationKey(receiver.ExportEncapsulationKey());

        (var ciphertext, var senderSecret) = sender.Encapsulate();
        var receiverSecret = receiver.Decapsulate(ciphertext);

        Assert.AreEqual(MLKem.SharedSecretSizeInBytes, senderSecret.Length);
        Assert.AreEqual(ExpectedCiphertextSize, ciphertext.Length);
        CollectionAssert.AreEqual(senderSecret, receiverSecret);
    }

    /// <summary>
    /// Verifies that a tampered ciphertext decapsulates without throwing but yields a shared secret different from
    /// the encapsulating party's — the FIPS 203 implicit-rejection contract.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextIsTampered_ShouldImplicitlyRejectWithoutThrowing()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        using var sender = new TKem();
        sender.ImportEncapsulationKey(kem.ExportEncapsulationKey());
        (var ciphertext, var senderSecret) = sender.Encapsulate();

        ciphertext[0] ^= 0x01;
        var rejected = kem.Decapsulate(ciphertext);

        Assert.AreEqual(MLKem.SharedSecretSizeInBytes, rejected.Length);
        CollectionAssert.AreNotEqual(senderSecret, rejected);
    }

    /// <summary>
    /// Verifies that implicit rejection is deterministic: the same tampered ciphertext yields the same rejection
    /// key on every decapsulation.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenSameTamperedCiphertextIsRepeated_ShouldYieldSameRejectionKey()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        using var sender = new TKem();
        sender.ImportEncapsulationKey(kem.ExportEncapsulationKey());
        (var ciphertext, _) = sender.Encapsulate();
        ciphertext[10] ^= 0xFF;

        CollectionAssert.AreEqual(kem.Decapsulate(ciphertext), kem.Decapsulate(ciphertext));
    }

    /// <summary>
    /// Verifies that the exported decapsulation key round-trips through
    /// <see cref="MLKem.ImportDecapsulationKey" /> and continues to decapsulate correctly.
    /// </summary>
    [TestMethod]
    public void ImportDecapsulationKey_WhenRoundTrippingExportedKey_ShouldDecapsulateCorrectly()
    {
        using var original = new TKem();
        original.GenerateKey();

        using var sender = new TKem();
        sender.ImportEncapsulationKey(original.ExportEncapsulationKey());
        (var ciphertext, var expectedSecret) = sender.Encapsulate();

        using var restored = new TKem();
        restored.ImportDecapsulationKey(original.ExportDecapsulationKey());

        Assert.IsTrue(restored.HasEncapsulationKey);
        CollectionAssert.AreEqual(original.ExportEncapsulationKey(), restored.ExportEncapsulationKey());
        CollectionAssert.AreEqual(expectedSecret, restored.Decapsulate(ciphertext));
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.ImportPrivateSeed" /> is deterministic: the same seed regenerates the same
    /// key pair.
    /// </summary>
    [TestMethod]
    public void ImportPrivateSeed_WhenSameSeedIsImportedTwice_ShouldRegenerateSameKeyPair()
    {
        var seed = new byte[MLKem.PrivateSeedSizeInBytes];
        new Random(203).NextBytes(seed);

        using var first = new TKem();
        using var second = new TKem();
        first.ImportPrivateSeed(seed);
        second.ImportPrivateSeed(seed);

        CollectionAssert.AreEqual(first.ExportEncapsulationKey(), second.ExportEncapsulationKey());
        CollectionAssert.AreEqual(first.ExportDecapsulationKey(), second.ExportDecapsulationKey());
    }

    /// <summary>
    /// Verifies that wrong-length arguments throw <see cref="ArgumentException" /> across the import, encapsulate,
    /// and decapsulate surfaces.
    /// </summary>
    [TestMethod]
    public void Members_WhenGivenWrongLengthArguments_ShouldThrowArgumentException()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        Assert.ThrowsExactly<ArgumentException>(() => { kem.ImportPrivateSeed(new byte[63]); });
        Assert.ThrowsExactly<ArgumentException>(() => { kem.ImportEncapsulationKey(new byte[ExpectedEncapsulationKeySize - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { kem.ImportDecapsulationKey(new byte[ExpectedDecapsulationKeySize + 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { _ = kem.Decapsulate(new byte[ExpectedCiphertextSize - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { kem.Encapsulate(new byte[ExpectedCiphertextSize - 1], new byte[32]); });
        Assert.ThrowsExactly<ArgumentException>(() => { kem.Encapsulate(new byte[ExpectedCiphertextSize], new byte[31]); });
    }

    /// <summary>
    /// Verifies that operations requiring missing key material throw <see cref="CryptographicException" />, and
    /// that an encapsulate-only instance cannot decapsulate or export the decapsulation key.
    /// </summary>
    [TestMethod]
    public void Members_WhenRequiredKeyMaterialIsMissing_ShouldThrowCryptographicException()
    {
        using var empty = new TKem();
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.Encapsulate(); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.Decapsulate(new byte[ExpectedCiphertextSize]); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.ExportEncapsulationKey(); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = empty.ExportDecapsulationKey(); });

        using var donor = new TKem();
        donor.GenerateKey();

        using var encapsulateOnly = new TKem();
        encapsulateOnly.ImportEncapsulationKey(donor.ExportEncapsulationKey());
        Assert.IsFalse(encapsulateOnly.HasDecapsulationKey);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = encapsulateOnly.Decapsulate(new byte[ExpectedCiphertextSize]);
        });
    }

    /// <summary>
    /// Verifies that members throw <see cref="ObjectDisposedException" /> after disposal and that double disposal
    /// is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeSubsequentMemberAccessThrowObjectDisposedException()
    {
        var kem = new TKem();
        kem.GenerateKey();
        var ciphertext = new byte[ExpectedCiphertextSize];

        kem.Dispose();
        kem.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = kem.HasDecapsulationKey; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { kem.GenerateKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = kem.Encapsulate(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = kem.Decapsulate(ciphertext); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = kem.ExportEncapsulationKey(); });
    }
}
