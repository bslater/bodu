// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests{T,T,T}.HashLengthInBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> returns a digest whose length
    /// matches <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void GetCurrentHash_WhenReturningDigest_ShouldHaveLengthMatchingHashLengthInBytes(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append([0xAB, 0xCD, 0xEF]);

        byte[] digest = algorithm.GetCurrentHash();

        Assert.HasCount(algorithm.HashLengthInBytes, digest);
    }
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" /> matches the specification
    /// declared for the given variant, both before and after hashing.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void HashLengthInBytes_WhenQueried_ShouldMatchSpecification(TVariant variant)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        TAlgorithm algorithm = CreateAlgorithm(variant);

        Assert.AreEqual(specification.HashLengthInBytes, algorithm.HashLengthInBytes);

        algorithm.Append([1, 2, 3, 4]);
        Assert.AreEqual(specification.HashLengthInBytes, algorithm.HashLengthInBytes);
    }

}
