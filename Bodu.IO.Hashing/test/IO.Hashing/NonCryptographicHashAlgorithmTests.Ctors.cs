// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="CreateAlgorithm(TVariant)" /> returns a non-null instance for every declared
    /// variant of <typeparamref name="TAlgorithm" />.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Ctor_WhenDefaultConstructed_ShouldReturnNonNullInstance(TVariant variant)
    {
        var algorithm = CreateAlgorithm(variant);
        Assert.IsNotNull(algorithm);
    }

    /// <summary>
    /// Verifies that the freshly constructed algorithm reports the
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" /> declared by the specification for the
    /// given variant.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Ctor_WhenDefaultConstructed_ShouldReportExpectedHashLength(TVariant variant)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        var algorithm = CreateAlgorithm(variant);

        Assert.AreEqual(specification.HashLengthInBytes, algorithm.HashLengthInBytes);
    }
}
