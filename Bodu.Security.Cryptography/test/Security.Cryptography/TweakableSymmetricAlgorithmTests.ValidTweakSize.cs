// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.ValidTweakSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ValidTweakSize(int)" /> returns
    /// <see langword="true" /> for every bit length that falls within <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" />.
    /// </summary>
    [TestMethod]
    public void ValidTweakSize_WhenLengthIsValid_ShouldReturnTrue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        foreach (KeySizes range in algorithm.LegalTweakSizes)
        {
            var step = range.SkipSize == 0 ? range.MaxSize - range.MinSize : range.SkipSize;

            for (var bits = range.MinSize; bits <= range.MaxSize; bits += step)
            {
                Assert.IsTrue(
                    algorithm.ValidTweakSize(bits),
                    $"ValidTweakSize({bits}) should return true for {typeof(TAlgorithm).Name}.");

                if (range.SkipSize == 0) break; // single legal value in this range
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ValidTweakSize(int)" /> returns
    /// <see langword="false" /> for bit lengths that fall outside all ranges in
    /// <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" />.
    /// Skips with <see cref="Assert.Inconclusive" /> when the algorithm accepts every
    /// byte-aligned length and no invalid size can be constructed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidTweakSizeBitsData))]
    public void ValidTweakSize_WhenLengthIsInvalid_ShouldReturnFalse(int tweakSize)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(
            algorithm.ValidTweakSize(tweakSize),
            $"ValidTweakSize({tweakSize}) should return false for {typeof(TAlgorithm).Name}.");
    }
}
