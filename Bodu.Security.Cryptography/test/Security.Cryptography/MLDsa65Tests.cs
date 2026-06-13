// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa65Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLDsa65" />, inheriting the asymmetric, signature, and ML-DSA family
/// contracts from <see cref="MLDsaContractTests{TTest, TDsa}" />.
/// </summary>
[TestClass]
public sealed partial class MLDsa65Tests
    : MLDsaContractTests<MLDsa65Tests, MLDsa65>
{
    /// <inheritdoc />
    protected override int SignatureSizeBytes => 3309;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 65,
            KeyExchangeAlgorithmName = null,
            SignatureAlgorithmName = "ML-DSA",
            PrivateKeySizeBytes = 4032,
            PublicKeySizeBytes = 1952,
        };

    /// <summary>
    /// Verifies that <see cref="MLDsa65.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using MLDsa65 dsa = MLDsa65.Create();

        Assert.IsNotNull(dsa);
        Assert.AreEqual(65, dsa.KeySize);
        Assert.IsFalse(dsa.HasPrivateKey);
    }
}
