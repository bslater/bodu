// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <typeparamref name="TAlgorithm" /> reports the correct <see cref="HashAlgorithm.HashSize" /> in bits
    /// when default-constructed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void HashSize_WhenDefaultConstructed_ShouldBeExpectedBitLength(TVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);
        var specification = GetSpecification(variant);
        Assert.AreEqual(specification.HashSize, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that <typeparamref name="TAlgorithm" /> reports an <see cref="HashAlgorithm.InputBlockSize" /> of at least
    /// one byte when default-constructed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void InputBlockSize_WhenDefaultConstructed_ShouldBePositive(TVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);
        var specification = GetSpecification(variant);
        Assert.AreEqual(specification.InputBlockSize, algorithm.InputBlockSize);
    }

    /// <summary>
    /// Verifies that <typeparamref name="TAlgorithm" /> reports an <see cref="HashAlgorithm.OutputBlockSize" /> of at least
    /// one byte when default-constructed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void OutputBlockSize_WhenDefaultConstructed_ShouldBePositive(TVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);
        var specification = GetSpecification(variant);
        Assert.AreEqual(specification.OutputBlockSize, algorithm.OutputBlockSize);
    }

    /// <summary>
    /// Verifies that <typeparamref name="TAlgorithm" /> supports reuse after hashing, as required by the
    /// <see cref="HashAlgorithm" /> contract.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void CanReuseTransform_WhenDefaultConstructed_ShouldBeTrue(TVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);
        var specification = GetSpecification(variant);
        Assert.AreEqual(specification.CanReuseTransform, algorithm.CanReuseTransform);
    }

    /// <summary>
    /// Verifies that <typeparamref name="TAlgorithm" /> supports transforming multiple blocks in a single call, as
    /// required by the <see cref="HashAlgorithm" /> contract.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void CanTransformMultipleBlocks_WhenDefaultConstructed_ShouldBeTrue(TVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);
        var specification = GetSpecification(variant);
        Assert.AreEqual(specification.CanTransformMultipleBlocks, algorithm.CanTransformMultipleBlocks);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Hash" /> is <see langword="null" /> on a freshly constructed
    /// <typeparamref name="TAlgorithm" /> before any data has been hashed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void Hash_WhenDefaultConstructed_ShouldBeNull(TVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);
        Assert.IsNull(algorithm.Hash);
    }
}
