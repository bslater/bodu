// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaContractTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA family contract tests, extending <see cref="SignatureAlgorithmTests{TTest, TAlgorithm}" />
/// with the FIPS 204 specifics: the 32-byte seed ξ, the private-key tr consistency check, hedged versus
/// deterministic signing, context-string binding and limits, the parameter-set size properties, and the
/// exact-length span overload.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TDsa">The concrete parameter-set type under test.</typeparam>
[TestClass]
public abstract class MLDsaContractTests<TTest, TDsa>
    : SignatureAlgorithmTests<TTest, TDsa>
    where TTest : MLDsaContractTests<TTest, TDsa>, new()
    where TDsa : MLDsa, new()
{
    /// <inheritdoc />
    protected sealed override void GenerateKey(TDsa algorithm) =>
        algorithm.GenerateKey();

    /// <inheritdoc />
    protected sealed override bool HasPrivateKey(TDsa algorithm) =>
        algorithm.HasPrivateKey;

    /// <inheritdoc />
    protected sealed override bool HasPublicKey(TDsa algorithm) =>
        algorithm.HasPublicKey;

    /// <inheritdoc />
    protected sealed override void ImportPrivateKey(TDsa algorithm, byte[] privateKey) =>
        algorithm.ImportPrivateKey(privateKey);

    /// <inheritdoc />
    protected sealed override void ImportPublicKey(TDsa algorithm, byte[] publicKey) =>
        algorithm.ImportPublicKey(publicKey);

    /// <inheritdoc />
    protected sealed override byte[] ExportPrivateKey(TDsa algorithm) =>
        algorithm.ExportPrivateKey();

    /// <inheritdoc />
    protected sealed override byte[] ExportPublicKey(TDsa algorithm) =>
        algorithm.ExportPublicKey();

    /// <inheritdoc />
    protected sealed override string GetAlgorithmName(TDsa algorithm) =>
        algorithm.AlgorithmName;

    /// <inheritdoc />
    protected sealed override int GetSecurityStrengthBits(TDsa algorithm) =>
        algorithm.SecurityStrengthBits;

    /// <inheritdoc />
    protected sealed override byte[] SignData(TDsa algorithm, byte[] data) =>
        algorithm.SignData(data);

    /// <inheritdoc />
    protected sealed override bool VerifyData(TDsa algorithm, byte[] data, byte[] signature) =>
        algorithm.VerifyData(data, signature);

    /// <summary>
    /// Verifies that the parameter-set size properties agree with the specification and the signature size hook.
    /// </summary>
    [TestMethod]
    public void SizeProperties_WhenRead_ShouldMatchSpecification()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using var dsa = new TDsa();

        Assert.AreEqual(spec.PublicKeySizeBytes, dsa.PublicKeySizeInBytes);
        Assert.AreEqual(spec.PrivateKeySizeBytes, dsa.PrivateKeySizeInBytes);
        Assert.AreEqual(SignatureSizeBytes, dsa.SignatureSizeInBytes);
        Assert.IsFalse(dsa.DeterministicSigning);
    }

    /// <summary>
    /// Verifies that hedged signing (the default) produces differing signatures across calls while
    /// <see cref="MLDsa.DeterministicSigning" /> reproduces the identical signature, and that both verify.
    /// </summary>
    [TestMethod]
    public void SignData_WhenHedgedVersusDeterministic_ShouldDifferAndReproduceRespectively()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        byte[] message = new byte[] { 42 };

        byte[] hedgedFirst = dsa.SignData(message);
        byte[] hedgedSecond = dsa.SignData(message);
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
        byte[] message = new byte[] { 7, 7, 7 };
        byte[] context = new byte[] { 0xC0, 0xFF, 0xEE };

        byte[] signature = dsa.SignData(message, context);

        Assert.IsTrue(dsa.VerifyData(message, signature, context));
        Assert.IsFalse(dsa.VerifyData(message, signature));
        Assert.IsFalse(dsa.VerifyData(message, signature, new byte[] { 0xC0, 0xFF, 0xEF }));
    }

    /// <summary>
    /// Verifies that contexts longer than the FIPS 204 255-byte limit throw <see cref="ArgumentException" /> on
    /// both the signing and verification surfaces.
    /// </summary>
    [TestMethod]
    public void SignAndVerify_WhenContextExceeds255Bytes_ShouldThrowArgumentException()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        byte[] oversized = new byte[MLDsa.MaxContextSizeInBytes + 1];

        Assert.ThrowsExactly<ArgumentException>(() => { _ = dsa.SignData(new byte[1], oversized); });
        Assert.ThrowsExactly<ArgumentException>(() => { _ = dsa.VerifyData(new byte[1], new byte[SignatureSizeBytes], oversized); });
    }

    /// <summary>
    /// Verifies that <see cref="MLDsa.ImportPrivateSeed" /> deterministically regenerates the same key pair from
    /// the same 32-byte seed ξ, and rejects seeds of any other length.
    /// </summary>
    [TestMethod]
    public void ImportPrivateSeed_WhenSeedIsReused_ShouldRegenerateSameKeyPairAndRejectWrongLengths()
    {
        byte[] seed = new byte[MLDsa.PrivateSeedSizeInBytes];
        new Random(204).NextBytes(seed);

        using var first = new TDsa();
        using var second = new TDsa();
        first.ImportPrivateSeed(seed);
        second.ImportPrivateSeed(seed);

        CollectionAssert.AreEqual(first.ExportPrivateKey(), second.ExportPrivateKey());
        CollectionAssert.AreEqual(first.ExportPublicKey(), second.ExportPublicKey());

        Assert.ThrowsExactly<ArgumentException>(() => { first.ImportPrivateSeed(new byte[MLDsa.PrivateSeedSizeInBytes - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { first.ImportPrivateSeed(new byte[MLDsa.PrivateSeedSizeInBytes + 1]); });
    }

    /// <summary>
    /// Verifies that <see cref="MLDsa.ImportPrivateKey" /> rejects a private key whose embedded public-key hash tr
    /// is inconsistent with its secret vectors.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenEmbeddedHashIsCorrupted_ShouldThrowArgumentException()
    {
        using var donor = new TDsa();
        donor.GenerateKey();

        // tr = H(pk, 64) occupies bytes 64-127 of the encoded private key.
        byte[] corrupted = donor.ExportPrivateKey();
        corrupted[70] ^= 0x01;

        using var dsa = new TDsa();
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dsa.ImportPrivateKey(corrupted);
        });

        Assert.AreEqual("privateKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MLDsa.ImportPrivateKey" /> rejects a private key whose t₀ vector has been corrupted,
    /// even though its embedded tr hash still matches. The public key is reconstructed from t₁ alone, so a corrupted
    /// t₀ leaves tr consistent; rejection therefore proves t₀ is validated by recomputation rather than only via tr.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenEmbeddedT0IsCorrupted_ShouldThrowArgumentException()
    {
        using var donor = new TDsa();
        donor.GenerateKey();

        // t₀ occupies the trailing section of the encoded private key; flipping its final byte changes a single
        // coefficient while leaving rho, K, tr, s1, and s2 — and thus the recomputed public key and its hash — intact.
        byte[] corrupted = donor.ExportPrivateKey();
        corrupted[^1] ^= 0x01;

        using var dsa = new TDsa();
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dsa.ImportPrivateKey(corrupted);
        });

        Assert.AreEqual("privateKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MLDsa.ImportPrivateKey" /> rejects a private key whose first s₁ coefficient is packed
    /// with a non-canonical code point outside the [−η, η] range, proving the import enforces strict canonical
    /// decoding rather than folding out-of-range values modulo q.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenS1PackingIsNonCanonical_ShouldThrowArgumentException()
    {
        using var donor = new TDsa();
        donor.GenerateKey();

        // s1 begins at byte 128 (after rho ‖ K ‖ tr). Forcing the first byte to 0xFF sets the leading η-bit code point
        // to all ones, which exceeds 2η for every parameter set and is therefore non-canonical.
        byte[] corrupted = donor.ExportPrivateKey();
        corrupted[128] = 0xFF;

        using var dsa = new TDsa();
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dsa.ImportPrivateKey(corrupted);
        });

        Assert.AreEqual("privateKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a single-byte mutation in the commitment hash c̃ (the first signature byte) causes verification
    /// to fail.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenCommitmentHashIsMutated_ShouldReturnFalse()
    {
        AssertMutatedSignatureFailsVerification(0);
    }

    /// <summary>
    /// Verifies that a single-byte mutation in the response z (a mid-signature byte) causes verification to fail.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenResponseIsMutated_ShouldReturnFalse()
    {
        AssertMutatedSignatureFailsVerification(SignatureSizeBytes / 2);
    }

    /// <summary>
    /// Verifies that a single-byte mutation in the hint section h (the final signature byte) causes verification to
    /// fail.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenHintSectionIsMutated_ShouldReturnFalse()
    {
        AssertMutatedSignatureFailsVerification(SignatureSizeBytes - 1);
    }

    /// <summary>
    /// Signs a fixed message deterministically, flips the bit at <paramref name="byteIndex" />, and asserts the
    /// mutated signature fails verification.
    /// </summary>
    /// <param name="byteIndex">The signature byte to mutate.</param>
    private void AssertMutatedSignatureFailsVerification(int byteIndex)
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        dsa.DeterministicSigning = true;
        byte[] message = new byte[] { 1, 2, 3 };

        byte[] signature = dsa.SignData(message);
        signature[byteIndex] ^= 0x01;

        Assert.IsFalse(dsa.VerifyData(message, signature));
    }

    /// <summary>
    /// Verifies that the unbounded Fiat-Shamir-with-aborts signing loop always terminates and produces a verifiable
    /// signature across a large number of hedged signatures, exercising the rejection-sampling restarts. The
    /// rejection-count distribution itself is not asserted, as observing it would require instrumenting the production
    /// signing path.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void SignData_WhenManyHedgedSignaturesProduced_ShouldAlwaysTerminateAndVerify()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();

        var random = new Random(204);
        byte[] message = new byte[64];

        for (int iteration = 0; iteration < 2_000; iteration++)
        {
            random.NextBytes(message);
            byte[] signature = dsa.SignData(message);
            Assert.IsTrue(dsa.VerifyData(message, signature), $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that a signature whose final hint count exceeds ω is rejected by the strict FIPS 204 hint decoding.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenHintCountExceedsOmega_ShouldReturnFalse()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        dsa.DeterministicSigning = true;
        byte[] message = new byte[] { 7 };

        byte[] signature = dsa.SignData(message);

        // The final signature byte is the cumulative hint count for the last polynomial; 0xFF = 255 exceeds ω
        // (at most 80) for every parameter set, so HintBitUnpack must reject the encoding.
        byte[] malformed = (byte[])signature.Clone();
        malformed[^1] = 0xFF;

        Assert.IsFalse(dsa.VerifyData(message, malformed));
    }

    /// <summary>
    /// Verifies that signing and verifying round-trip for context strings at the length boundaries 0, 1, and the
    /// FIPS 204 maximum of 255 bytes.
    /// </summary>
    /// <param name="contextLength">The context length to exercise.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(255)]
    public void SignAndVerify_WhenContextLengthIsAtBoundary_ShouldRoundTrip(int contextLength)
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        byte[] message = new byte[] { 9, 9 };
        byte[] context = new byte[contextLength];
        new Random(contextLength + 1).NextBytes(context);

        byte[] signature = dsa.SignData(message, context);

        Assert.IsTrue(dsa.VerifyData(message, signature, context));
    }

    /// <summary>
    /// Verifies that the exact-length span overload of <see cref="MLDsa.SignData(ReadOnlySpan{byte})" /> matches
    /// the allocating overload in deterministic mode and rejects destinations of any other length.
    /// </summary>
    [TestMethod]
    public void SignData_WhenUsingSpanOverload_ShouldMatchAllocatingOverloadAndRejectWrongDestinationLengths()
    {
        using var dsa = new TDsa();
        dsa.GenerateKey();
        dsa.DeterministicSigning = true;
        byte[] message = new byte[] { 1, 2, 3 };

        byte[] allocating = dsa.SignData(message);
        byte[] spanResult = new byte[SignatureSizeBytes];
        dsa.SignData(message, ReadOnlySpan<byte>.Empty, spanResult);
        CollectionAssert.AreEqual(allocating, spanResult);

        Assert.ThrowsExactly<ArgumentException>(() => { dsa.SignData(message, ReadOnlySpan<byte>.Empty, new byte[SignatureSizeBytes - 1]); });
        Assert.ThrowsExactly<ArgumentException>(() => { dsa.SignData(message, ReadOnlySpan<byte>.Empty, new byte[SignatureSizeBytes + 1]); });
    }
}
