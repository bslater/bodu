// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.HashLengthInBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" /> matches the specification
    /// declared for the given variant, both before and after hashing.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void HashLengthInBytes_WhenQueried_ShouldMatchSpecification(TVariant variant)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        var algorithm = CreateAlgorithm(variant);

        Assert.AreEqual(specification.HashLengthInBytes, algorithm.HashLengthInBytes);

        algorithm.Append(new byte[] { 1, 2, 3, 4 });
        Assert.AreEqual(specification.HashLengthInBytes, algorithm.HashLengthInBytes);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> returns a digest whose length
    /// matches <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetCurrentHash_WhenReturningDigest_ShouldHaveLengthMatchingHashLengthInBytes(TVariant variant)
    {
        var algorithm = CreateAlgorithm(variant);
        algorithm.Append(new byte[] { 0xAB, 0xCD, 0xEF });

        byte[] digest = algorithm.GetCurrentHash();

        Assert.AreEqual(algorithm.HashLengthInBytes, digest.Length);
    }
}
