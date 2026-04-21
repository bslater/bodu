// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.GetHashCode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="object.GetHashCode" /> returns a stable value across repeated invocations on the
    /// same instance without mutating any observable hashing state.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetHashCode_WhenCalledRepeatedly_ShouldReturnConsistentValue(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);

        int first = algorithm.GetHashCode();
        int second = algorithm.GetHashCode();

        Assert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that invoking <see cref="object.GetHashCode" /> does not disturb the accumulator — the digest
    /// observed immediately before and after the call is identical.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetHashCode_WhenInvoked_ShouldNotMutateAccumulatorState(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(SharedInputs["ABC"]);

        byte[] before = algorithm.GetCurrentHash();
        _ = algorithm.GetHashCode();
        byte[] after = algorithm.GetCurrentHash();

        CollectionAssert.AreEqual(before, after);
    }
}
