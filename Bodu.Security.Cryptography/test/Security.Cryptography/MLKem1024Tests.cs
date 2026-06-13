// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem1024Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLKem1024" />, inheriting the asymmetric, KEM, and ML-KEM family contracts
/// from <see cref="MLKemContractTests{TTest, TKem}" />.
/// </summary>
[TestClass]
public sealed partial class MLKem1024Tests
    : MLKemContractTests<MLKem1024Tests, MLKem1024>
{
    /// <inheritdoc />
    protected override int CiphertextSizeBytes => 1568;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 1024,
            KeyExchangeAlgorithmName = "ML-KEM",
            SignatureAlgorithmName = null,
            PrivateKeySizeBytes = 3168,
            PublicKeySizeBytes = 1568,
        };

    /// <summary>
    /// Verifies that <see cref="MLKem1024.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using var kem = MLKem1024.Create();

        Assert.IsNotNull(kem);
        Assert.AreEqual(1024, kem.KeySize);
        Assert.IsFalse(kem.HasDecapsulationKey);
    }
}
