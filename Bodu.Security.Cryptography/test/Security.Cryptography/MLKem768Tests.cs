// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem768Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLKem768" />, inheriting the asymmetric, KEM, and ML-KEM family contracts
/// from <see cref="MLKemContractTests{TTest, TKem}" />.
/// </summary>
[TestClass]
public sealed partial class MLKem768Tests
    : MLKemContractTests<MLKem768Tests, MLKem768>
{
    /// <inheritdoc />
    protected override int CiphertextSizeBytes => 1088;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 768,
            KeyExchangeAlgorithmName = "ML-KEM",
            SignatureAlgorithmName = null,
            PrivateKeySizeBytes = 2400,
            PublicKeySizeBytes = 1184,
        };

    /// <summary>
    /// Verifies that <see cref="MLKem768.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using var kem = MLKem768.Create();

        Assert.IsNotNull(kem);
        Assert.AreEqual(768, kem.KeySize);
        Assert.IsFalse(kem.HasDecapsulationKey);
    }
}
