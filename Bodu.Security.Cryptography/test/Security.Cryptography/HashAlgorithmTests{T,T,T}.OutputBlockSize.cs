// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests{T,T,T}.OutputBlockSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.OutputBlockSize" /> returns the expected default block size.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void OutputBlockSize_ShouldBeExpectedOutputBlockSize(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);
        using TAlgorithm algorithm = CreateAlgorithm(variant);
        Assert.AreEqual(specification.OutputBlockSize, algorithm.OutputBlockSize, $"{typeof(TAlgorithm).Name} OutputBlockSize should be {specification.OutputBlockSize}.");
    }
}
