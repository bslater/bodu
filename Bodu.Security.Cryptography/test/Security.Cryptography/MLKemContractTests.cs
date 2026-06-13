// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM family contract tests, extending <see cref="KemAlgorithmTests{TTest, TAlgorithm}" /> with
/// the FIPS 203 specifics: the 64-byte private seed, the §7.2 / §7.3 import checks, the parameter-set size
/// properties, and the exact-length span overloads.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TKem">The concrete parameter-set type under test.</typeparam>
[TestClass]
public abstract class MLKemContractTests<TTest, TKem>
    : KemAlgorithmTests<TTest, TKem>
    where TTest : MLKemContractTests<TTest, TKem>, new()
    where TKem : MLKem, new()
{
    /// <inheritdoc />
    protected sealed override int SharedSecretSizeBytes =>
        MLKem.SharedSecretSizeInBytes;

    /// <inheritdoc />
    protected sealed override void GenerateKey(TKem algorithm) =>
        algorithm.GenerateKey();

    /// <inheritdoc />
    protected sealed override bool HasPrivateKey(TKem algorithm) =>
        algorithm.HasDecapsulationKey;

    /// <inheritdoc />
    protected sealed override bool HasPublicKey(TKem algorithm) =>
        algorithm.HasEncapsulationKey;

    /// <inheritdoc />
    protected sealed override void ImportPrivateKey(TKem algorithm, byte[] privateKey) =>
        algorithm.ImportDecapsulationKey(privateKey);

    /// <inheritdoc />
    protected sealed override void ImportPublicKey(TKem algorithm, byte[] publicKey) =>
        algorithm.ImportEncapsulationKey(publicKey);

    /// <inheritdoc />
    protected sealed override byte[] ExportPrivateKey(TKem algorithm) =>
        algorithm.ExportDecapsulationKey();

    /// <inheritdoc />
    protected sealed override byte[] ExportPublicKey(TKem algorithm) =>
        algorithm.ExportEncapsulationKey();

    /// <inheritdoc />
    protected sealed override (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(TKem algorithm) =>
        algorithm.Encapsulate();

    /// <inheritdoc />
    protected sealed override byte[] Decapsulate(TKem algorithm, byte[] ciphertext) =>
        algorithm.Decapsulate(ciphertext);

    /// <summary>
    /// Verifies that the parameter-set size properties agree with the specification and the KEM size hooks.
    /// </summary>
    [TestMethod]
    public void SizeProperties_WhenRead_ShouldMatchSpecification()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using var kem = new TKem();

        Assert.AreEqual(spec.PublicKeySizeBytes, kem.EncapsulationKeySizeInBytes);
        Assert.AreEqual(spec.PrivateKeySizeBytes, kem.DecapsulationKeySizeInBytes);
        Assert.AreEqual(CiphertextSizeBytes, kem.CiphertextSizeInBytes);
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.ImportPrivateSeed" /> deterministically regenerates the same key pair from
    /// the same 64-byte d ‖ z seed, and rejects seeds of any other length.
    /// </summary>
    [TestMethod]
    public void ImportPrivateSeed_WhenSeedIsReused_ShouldRegenerateSameKeyPairAndRejectWrongLengths()
    {
        var seed = new byte[MLKem.PrivateSeedSizeInBytes];
        new Random(203).NextBytes(seed);

        using var first = new TKem();
        using var second = new TKem();
        first.ImportPrivateSeed(seed);
        second.ImportPrivateSeed(seed);

        CollectionAssert.AreEqual(first.ExportEncapsulationKey(), second.ExportEncapsulationKey());
        CollectionAssert.AreEqual(first.ExportDecapsulationKey(), second.ExportDecapsulationKey());

        Assert.ThrowsExactly<ArgumentException>(() => { first.ImportPrivateSeed(new byte[MLKem.PrivateSeedSizeInBytes - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { first.ImportPrivateSeed(new byte[MLKem.PrivateSeedSizeInBytes + 1]); });
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.ImportDecapsulationKey" /> applies the FIPS 203 §7.3 hash-consistency check,
    /// rejecting a decapsulation key whose embedded H(ek) digest has been corrupted.
    /// </summary>
    [TestMethod]
    public void ImportDecapsulationKey_WhenEmbeddedHashIsCorrupted_ShouldThrowArgumentException()
    {
        using var donor = new TKem();
        donor.GenerateKey();

        // The H(ek) digest sits immediately after dk_PKE ‖ ek, i.e. 32 bytes before the trailing z seed.
        var corrupted = donor.ExportDecapsulationKey();
        corrupted[^33] ^= 0x01;

        using var kem = new TKem();
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            kem.ImportDecapsulationKey(corrupted);
        });

        Assert.AreEqual("decapsulationKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.ImportEncapsulationKey" /> applies the FIPS 203 §7.2 modulus check, rejecting
    /// an encapsulation key carrying a 12-bit coefficient at or above q.
    /// </summary>
    [TestMethod]
    public void ImportEncapsulationKey_WhenCoefficientIsNotReduced_ShouldThrowArgumentException()
    {
        using var donor = new TKem();
        donor.GenerateKey();

        // Force the first packed coefficient to 0xFFF = 4095 >= q = 3329.
        var corrupted = donor.ExportEncapsulationKey();
        corrupted[0] = 0xFF;
        corrupted[1] |= 0x0F;

        using var kem = new TKem();
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            kem.ImportEncapsulationKey(corrupted);
        });

        Assert.AreEqual("encapsulationKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the exact-length span overloads of <see cref="MLKem.Encapsulate()" /> and
    /// <see cref="MLKem.Decapsulate(ReadOnlySpan{byte})" /> round-trip correctly and reject destinations of any
    /// other length.
    /// </summary>
    [TestMethod]
    public void SpanOverloads_WhenUsed_ShouldRoundTripAndRejectWrongDestinationLengths()
    {
        using var receiver = new TKem();
        receiver.GenerateKey();

        using var sender = new TKem();
        sender.ImportEncapsulationKey(receiver.ExportEncapsulationKey());

        var ciphertext = new byte[CiphertextSizeBytes];
        var senderSecret = new byte[MLKem.SharedSecretSizeInBytes];
        sender.Encapsulate(ciphertext, senderSecret);

        var receiverSecret = new byte[MLKem.SharedSecretSizeInBytes];
        receiver.Decapsulate(ciphertext, receiverSecret);
        CollectionAssert.AreEqual(senderSecret, receiverSecret);

        Assert.ThrowsExactly<ArgumentException>(() => { sender.Encapsulate(new byte[CiphertextSizeBytes - 1], new byte[32]); });
        Assert.ThrowsExactly<ArgumentException>(() => { sender.Encapsulate(new byte[CiphertextSizeBytes], new byte[31]); });
        Assert.ThrowsExactly<ArgumentException>(() => { receiver.Decapsulate(ciphertext, new byte[33]); });
    }
}
