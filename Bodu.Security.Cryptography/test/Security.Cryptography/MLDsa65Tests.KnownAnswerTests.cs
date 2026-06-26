// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa65Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks <see cref="MLDsa65" /> against the embedded NIST ACVP ML-DSA-65 known-answer vectors.
/// </summary>
public partial class MLDsa65Tests
{
    /// <summary>
    /// Yields the ACVP key-generation vectors for ML-DSA-65.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> KeyGenVectors() =>
        MLDsaAcvpVectors.KeyGen("ML-DSA-65");

    /// <summary>
    /// Yields the ACVP deterministic and hedged signature-generation vectors for ML-DSA-65.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> SigGenVectors() =>
        MLDsaAcvpVectors.SigGen("ML-DSA-65");

    /// <summary>
    /// Yields the ACVP signature-verification vectors for ML-DSA-65, including tampered rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> SigVerVectors() =>
        MLDsaAcvpVectors.SigVer("ML-DSA-65");

    /// <summary>
    /// Verifies that <see cref="MLDsa.ImportPrivateSeed" /> reproduces the expected encoded key pair for each ACVP
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
        using var dsa = new MLDsa65();
        MLDsaAcvpVectors.AssertKeyGen(dsa, vector);
    }

    /// <summary>
    /// Verifies that signing reproduces the expected signature for each ACVP sigGen vector, driving deterministic
    /// rows through the public surface and hedged rows through the internal explicit-randomness interface.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(SigGenVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SignData_WhenGivenAcvpSigGenVector_ShouldReproduceExpectedSignature(SignatureKnownAnswer vector)
    {
        using var dsa = new MLDsa65();
        MLDsaAcvpVectors.AssertSigGen(dsa, vector);
    }

    /// <summary>
    /// Verifies that verification reaches the verdict mandated by each ACVP sigVer vector, covering modified z,
    /// hint, commitment, and message rows.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(SigVerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void VerifyData_WhenGivenAcvpSigVerVector_ShouldReachMandatedVerdict(SignatureKnownAnswer vector)
    {
        using var dsa = new MLDsa65();
        MLDsaAcvpVectors.AssertSigVer(dsa, vector);
    }
}
