// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem768Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks <see cref="MLKem768" /> against the embedded NIST ACVP ML-KEM-768 known-answer vectors.
/// </summary>
public partial class MLKem768Tests
{
    /// <summary>
    /// Yields the ACVP key-generation vectors for ML-KEM-768.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> KeyGenVectors() =>
        MLKemAcvpVectors.KeyGen("ML-KEM-768");

    /// <summary>
    /// Yields the ACVP fixed-randomness encapsulation vectors for ML-KEM-768.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> EncapsulationVectors() =>
        MLKemAcvpVectors.EncapDecap("ML-KEM-768", "encapsulation");

    /// <summary>
    /// Yields the ACVP decapsulation vectors for ML-KEM-768, including modified-ciphertext implicit-rejection rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> DecapsulationVectors() =>
        MLKemAcvpVectors.EncapDecap("ML-KEM-768", "decapsulation");

    /// <summary>
    /// Yields the ACVP key-import validation vectors for ML-KEM-768.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> KeyCheckVectors() =>
        MLKemAcvpVectors.KeyCheck("ML-KEM-768");

    /// <summary>
    /// Verifies that <see cref="MLKem.ImportPrivateSeed" /> reproduces the expected encoded key pair for each ACVP
    /// keyGen vector.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(KeyGenVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportPrivateSeed_WhenGivenAcvpKeyGenVector_ShouldReproduceExpectedKeyPair(KeyGenKnownAnswer vector)
    {
        using var kem = new MLKem768();
        MLKemAcvpVectors.AssertKeyGen(kem, vector);
    }

    /// <summary>
    /// Verifies that the engine reproduces the expected ciphertext and shared secret for each ACVP encapsulation
    /// vector when driven with the vector's fixed randomness.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(EncapsulationVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encapsulate_WhenGivenAcvpVectorWithFixedRandomness_ShouldReproduceExpectedCiphertextAndSecret(KemKnownAnswer vector)
    {
        MLKemAcvpVectors.AssertEncapsulation(MLKemAcvpVectors.ResolveParameters("ML-KEM-768"), vector);
    }

    /// <summary>
    /// Verifies that <see cref="MLKem.Decapsulate(ReadOnlySpan{byte})" /> yields the expected shared secret for
    /// each ACVP decapsulation vector, including the implicit-rejection key for modified ciphertexts.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DecapsulationVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decapsulate_WhenGivenAcvpVector_ShouldYieldExpectedSharedSecret(KemKnownAnswer vector)
    {
        using var kem = new MLKem768();
        MLKemAcvpVectors.AssertDecapsulation(kem, vector);
    }

    /// <summary>
    /// Verifies that key import accepts or rejects each ACVP key-check vector per the FIPS 203 §7.2 / §7.3 rules.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(KeyCheckVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportKey_WhenGivenAcvpKeyCheckVector_ShouldAcceptOrRejectPerFips203(KeyValidationKnownAnswer vector)
    {
        using var kem = new MLKem768();
        MLKemAcvpVectors.AssertKeyCheck(kem, vector);
    }
}
