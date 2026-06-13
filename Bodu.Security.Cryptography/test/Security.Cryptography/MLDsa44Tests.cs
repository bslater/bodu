// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa44Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLDsa44" />, inheriting the asymmetric, signature, and ML-DSA family
/// contracts from <see cref="MLDsaContractTests{TTest, TDsa}" />.
/// </summary>
[TestClass]
public sealed partial class MLDsa44Tests
    : MLDsaContractTests<MLDsa44Tests, MLDsa44>
{
    /// <inheritdoc />
    protected override int SignatureSizeBytes => 2420;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 44,
            KeyExchangeAlgorithmName = null,
            SignatureAlgorithmName = "ML-DSA",
            PrivateKeySizeBytes = 2560,
            PublicKeySizeBytes = 1312,
        };

    /// <summary>
    /// Verifies that <see cref="MLDsa44.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using var dsa = MLDsa44.Create();

        Assert.IsNotNull(dsa);
        Assert.AreEqual(44, dsa.KeySize);
        Assert.IsFalse(dsa.HasPrivateKey);
    }
}
