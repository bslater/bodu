// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa65Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLDsa65" />, inheriting the shared behavioral contract from
/// <see cref="MLDsaContractTests{TDsa}" />.
/// </summary>
[TestClass]
public partial class MLDsa65Tests
    : MLDsaContractTests<MLDsa65>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeDesignator => 65;

    /// <inheritdoc />
    protected override int ExpectedPublicKeySize => 1952;

    /// <inheritdoc />
    protected override int ExpectedPrivateKeySize => 4032;

    /// <inheritdoc />
    protected override int ExpectedSignatureSize => 3309;

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
