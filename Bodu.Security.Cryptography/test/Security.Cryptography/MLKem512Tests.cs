// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem512Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLKem512" />, inheriting the shared behavioral contract from
/// <see cref="MLKemContractTests{TKem}" />.
/// </summary>
[TestClass]
public partial class MLKem512Tests
    : MLKemContractTests<MLKem512>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeDesignator => 512;

    /// <inheritdoc />
    protected override int ExpectedEncapsulationKeySize => 800;

    /// <inheritdoc />
    protected override int ExpectedDecapsulationKeySize => 1632;

    /// <inheritdoc />
    protected override int ExpectedCiphertextSize => 768;

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
