// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa44Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLDsa44" />, inheriting the shared behavioral contract from
/// <see cref="MLDsaContractTests{TDsa}" />.
/// </summary>
[TestClass]
public partial class MLDsa44Tests
    : MLDsaContractTests<MLDsa44>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeDesignator => 44;

    /// <inheritdoc />
    protected override int ExpectedPublicKeySize => 1312;

    /// <inheritdoc />
    protected override int ExpectedPrivateKeySize => 2560;

    /// <inheritdoc />
    protected override int ExpectedSignatureSize => 2420;

    /// <summary>
    /// Verifies that <see cref="MLDsa44.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using MLDsa44 dsa = MLDsa44.Create();

        Assert.IsNotNull(dsa);
        Assert.AreEqual(44, dsa.KeySize);
        Assert.IsFalse(dsa.HasPrivateKey);
    }
}
