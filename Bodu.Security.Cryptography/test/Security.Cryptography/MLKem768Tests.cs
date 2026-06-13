// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem768Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLKem768" />, inheriting the shared behavioral contract from
/// <see cref="MLKemContractTests{TKem}" />.
/// </summary>
[TestClass]
public partial class MLKem768Tests
    : MLKemContractTests<MLKem768>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeDesignator => 768;

    /// <inheritdoc />
    protected override int ExpectedEncapsulationKeySize => 1184;

    /// <inheritdoc />
    protected override int ExpectedDecapsulationKeySize => 2400;

    /// <inheritdoc />
    protected override int ExpectedCiphertextSize => 1088;

    /// <summary>
    /// Verifies that <see cref="MLKem768.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using MLKem768 kem = MLKem768.Create();

        Assert.IsNotNull(kem);
        Assert.AreEqual(768, kem.KeySize);
        Assert.IsFalse(kem.HasDecapsulationKey);
    }
}
