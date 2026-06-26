// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemContractTests{T,T}.cs" company="Bodu Pty. Ltd.">
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
    protected sealed override string GetAlgorithmName(TKem algorithm) =>
        algorithm.AlgorithmName;

    /// <inheritdoc />
    protected sealed override int GetSecurityStrengthBits(TKem algorithm) =>
        algorithm.SecurityStrengthBits;

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
        byte[] seed = new byte[MLKem.PrivateSeedSizeInBytes];
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
        byte[] corrupted = donor.ExportDecapsulationKey();
        corrupted[^33] ^= 0x01;

        using var kem = new TKem();
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
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
        byte[] corrupted = donor.ExportEncapsulationKey();
        corrupted[0] = 0xFF;
        corrupted[1] |= 0x0F;

        using var kem = new TKem();
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            kem.ImportEncapsulationKey(corrupted);
        });

        Assert.AreEqual("encapsulationKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.ImportDecapsulationKey" /> rejects a decapsulation key whose embedded
    /// encapsulation key carries a non-reduced coefficient, even when its H(ek) digest has been regenerated to match.
    /// This proves the §7.3 check validates the embedded ek's modulus and is not satisfied by the hash alone.
    /// </summary>
    [TestMethod]
    public void ImportDecapsulationKey_WhenEmbeddedKeyNonCanonicalButHashConsistent_ShouldThrowArgumentException()
    {
        using var donor = new TKem();
        donor.GenerateKey();

        int encapsulationKeySize = donor.EncapsulationKeySizeInBytes;
        int k = (encapsulationKeySize - 32) / 384;
        int ekOffset = 384 * k;
        int hashOffset = ekOffset + encapsulationKeySize;

        byte[] corrupted = donor.ExportDecapsulationKey();

        // Force the embedded ek's first packed coefficient to 0xFFF = 4095 >= q = 3329.
        corrupted[ekOffset] = 0xFF;
        corrupted[ekOffset + 1] |= 0x0F;

        // Regenerate H(ek) over the modified ek so the hash check would pass, isolating the modulus check.
        Span<byte> regeneratedHash = stackalloc byte[32];
        KeccakSponge.Sha3_256(corrupted.AsSpan(ekOffset, encapsulationKeySize), regeneratedHash);
        regeneratedHash.CopyTo(corrupted.AsSpan(hashOffset, 32));

        using var kem = new TKem();
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            kem.ImportDecapsulationKey(corrupted);
        });

        Assert.AreEqual("decapsulationKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.Decapsulate(ReadOnlySpan{byte})" /> rejects an empty ciphertext.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextIsEmpty_ShouldThrowArgumentException()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        Assert.ThrowsExactly<ArgumentException>(() => { _ = kem.Decapsulate(Array.Empty<byte>()); });
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.Decapsulate(ReadOnlySpan{byte})" /> rejects a ciphertext one byte shorter than
    /// the parameter set's exact size.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextIsOneByteTooShort_ShouldThrowArgumentException()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        Assert.ThrowsExactly<ArgumentException>(() => { _ = kem.Decapsulate(new byte[CiphertextSizeBytes - 1]); });
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.Decapsulate(ReadOnlySpan{byte})" /> rejects a ciphertext one byte longer than
    /// the parameter set's exact size.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextIsOneByteTooLong_ShouldThrowArgumentException()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        Assert.ThrowsExactly<ArgumentException>(() => { _ = kem.Decapsulate(new byte[CiphertextSizeBytes + 1]); });
    }

    /// <summary>
    /// Verifies the FIPS 203 implicit-rejection contract: decapsulating a tampered ciphertext of the correct length
    /// does not throw.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextIsTampered_ShouldNotThrow()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        (byte[] ciphertext, _) = kem.Encapsulate();
        ciphertext[0] ^= 0x01;

        byte[] implicitSecret = kem.Decapsulate(ciphertext);

        Assert.AreEqual(MLKem.SharedSecretSizeInBytes, implicitSecret.Length);
    }

    /// <summary>
    /// Verifies the FIPS 203 implicit-rejection contract: a tampered ciphertext yields a shared secret unrelated to
    /// the one bound to the genuine ciphertext.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenCiphertextIsTampered_ShouldReturnDifferentSecret()
    {
        using var kem = new TKem();
        kem.GenerateKey();

        (byte[] ciphertext, _) = kem.Encapsulate();
        byte[] genuine = kem.Decapsulate(ciphertext);

        byte[] tampered = (byte[])ciphertext.Clone();
        tampered[0] ^= 0x01;

        CollectionAssert.AreNotEqual(genuine, kem.Decapsulate(tampered));
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

        byte[] ciphertext = new byte[CiphertextSizeBytes];
        byte[] senderSecret = new byte[MLKem.SharedSecretSizeInBytes];
        sender.Encapsulate(ciphertext, senderSecret);

        byte[] receiverSecret = new byte[MLKem.SharedSecretSizeInBytes];
        receiver.Decapsulate(ciphertext, receiverSecret);
        CollectionAssert.AreEqual(senderSecret, receiverSecret);

        Assert.ThrowsExactly<ArgumentException>(() => { sender.Encapsulate(new byte[CiphertextSizeBytes - 1], new byte[32]); });
        Assert.ThrowsExactly<ArgumentException>(() => { sender.Encapsulate(new byte[CiphertextSizeBytes], new byte[31]); });
        Assert.ThrowsExactly<ArgumentException>(() => { receiver.Decapsulate(ciphertext, new byte[33]); });
    }
}
