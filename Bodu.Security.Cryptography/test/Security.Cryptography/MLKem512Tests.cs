// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem512Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLKem512" />, inheriting the asymmetric, KEM, and ML-KEM family contracts
/// from <see cref="MLKemContractTests{TTest, TKem}" />.
/// </summary>
[TestClass]
public sealed partial class MLKem512Tests
    : MLKemContractTests<MLKem512Tests, MLKem512>
{
    /// <inheritdoc />
    protected override int CiphertextSizeBytes => 768;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 512,
            KeyExchangeAlgorithmName = "ML-KEM",
            SignatureAlgorithmName = null,
            PrivateKeySizeBytes = 1632,
            PublicKeySizeBytes = 800,
        };

    /// <summary>
    /// Verifies that <see cref="MLKem512.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using MLKem512 kem = MLKem512.Create();

        Assert.IsNotNull(kem);
        Assert.AreEqual(512, kem.KeySize);
        Assert.IsFalse(kem.HasDecapsulationKey);
    }
}
