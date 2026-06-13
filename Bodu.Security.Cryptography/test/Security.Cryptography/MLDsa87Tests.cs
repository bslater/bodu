// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa87Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLDsa87" />, inheriting the asymmetric, signature, and ML-DSA family
/// contracts from <see cref="MLDsaContractTests{TTest, TDsa}" />.
/// </summary>
[TestClass]
public sealed partial class MLDsa87Tests
    : MLDsaContractTests<MLDsa87Tests, MLDsa87>
{
    /// <inheritdoc />
    protected override int SignatureSizeBytes => 4627;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 87,
            KeyExchangeAlgorithmName = null,
            SignatureAlgorithmName = "ML-DSA",
            PrivateKeySizeBytes = 4896,
            PublicKeySizeBytes = 2592,
        };

    /// <summary>
    /// Verifies that <see cref="MLDsa87.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using MLDsa87 dsa = MLDsa87.Create();

        Assert.IsNotNull(dsa);
        Assert.AreEqual(87, dsa.KeySize);
        Assert.IsFalse(dsa.HasPrivateKey);
    }
}
