// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.Reset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.Reset" /> after appending data returns the
    /// algorithm to the same state as a freshly constructed instance.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Reset_AfterAppend_ShouldRestoreInitialState(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.QuickBrownFox);
        algorithm.Reset();

        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);

        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that calling <see cref="NonCryptographicHashAlgorithm.Reset" /> on a freshly constructed
    /// instance is a no-op and leaves the empty-input digest unchanged.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Reset_OnFreshInstance_ShouldBeNoOp(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        var before = algorithm.GetCurrentHash();

        algorithm.Reset();

        CollectionAssert.AreEqual(before, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that append / reset / append cycles produce the same digest as the final append in isolation,
    /// confirming that <see cref="NonCryptographicHashAlgorithm.Reset" /> clears any residual buffering state.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Reset_BetweenAppends_ShouldDiscardAccumulatedInput(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Sequential0To255);
        algorithm.Reset();
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        NonCryptographicHashAlgorithm direct = CreateAlgorithm(variant);
        direct.Append(NonCryptographicHashSharedInputs.Abc);

        CollectionAssert.AreEqual(direct.GetCurrentHash(), algorithm.GetCurrentHash());
    }
}
