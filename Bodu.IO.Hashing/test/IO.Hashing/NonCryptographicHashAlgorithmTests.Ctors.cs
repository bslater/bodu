// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that the freshly constructed algorithm reports the
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" /> declared by the specification for the
    /// given variant.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void Ctor_WhenDefaultConstructed_ShouldReportExpectedHashLength(TVariant variant)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        TAlgorithm algorithm = CreateAlgorithm(variant);

        Assert.AreEqual(specification.HashLengthInBytes, algorithm.HashLengthInBytes);
    }
    /// <summary>
    /// Verifies that <see cref="CreateAlgorithm(TVariant)" /> returns a non-null instance for every declared
    /// variant of <typeparamref name="TAlgorithm" />.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void Ctor_WhenDefaultConstructed_ShouldReturnNonNullInstance(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        Assert.IsNotNull(algorithm);
    }

}
