// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem1024Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLKem1024" />, inheriting the shared behavioral contract from
/// <see cref="MLKemContractTests{TKem}" />.
/// </summary>
[TestClass]
public partial class MLKem1024Tests
    : MLKemContractTests<MLKem1024>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeDesignator => 1024;

    /// <inheritdoc />
    protected override int ExpectedEncapsulationKeySize => 1568;

    /// <inheritdoc />
    protected override int ExpectedDecapsulationKeySize => 3168;

    /// <inheritdoc />
    protected override int ExpectedCiphertextSize => 1568;

    /// <summary>
    /// Verifies that <see cref="MLKem1024.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using MLKem1024 kem = MLKem1024.Create();

        Assert.IsNotNull(kem);
        Assert.AreEqual(1024, kem.KeySize);
        Assert.IsFalse(kem.HasDecapsulationKey);
    }
}
