// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests{T,T,T}.GetHashCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that <see cref="object.GetHashCode" /> throws <see cref="NotImplementedException" />,
    /// confirming that the algorithm does not override the method inherited from
    /// <see cref="NonCryptographicHashAlgorithm" />.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void GetHashCode_WhenCalled_ShouldThrowExactly(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = algorithm.GetHashCode();
        });
    }

}
