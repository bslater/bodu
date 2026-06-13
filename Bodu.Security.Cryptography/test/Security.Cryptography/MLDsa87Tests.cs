// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa87Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLDsa87" />, inheriting the shared behavioral contract from
/// <see cref="MLDsaContractTests{TDsa}" />.
/// </summary>
[TestClass]
public partial class MLDsa87Tests
    : MLDsaContractTests<MLDsa87>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeDesignator => 87;

    /// <inheritdoc />
    protected override int ExpectedPublicKeySize => 2592;

    /// <inheritdoc />
    protected override int ExpectedPrivateKeySize => 4896;

    /// <inheritdoc />
    protected override int ExpectedSignatureSize => 4627;

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
