// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.InputBlockSize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.InputBlockSize" /> returns the expected default block size.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void InputBlockSize_ShouldBeExpectedInputBlockSize(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);
        using TAlgorithm algorithm = CreateAlgorithm(variant);
        Assert.AreEqual(specification.InputBlockSize, algorithm.InputBlockSize, $"{typeof(TAlgorithm).Name} InputBlockSize should be {specification.InputBlockSize}.");
    }
}
