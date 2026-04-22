// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.ValidTweakSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        using var algorithm = CreateAlgorithm();

        foreach (var range in algorithm.LegalTweakSizes)
        {
            int step = range.SkipSize == 0 ? range.MaxSize - range.MinSize : range.SkipSize;

            for (int bits = range.MinSize; bits <= range.MaxSize; bits += step)
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
    public void ValidTweakSize_WhenLengthIsInvalid_ShouldReturnFalse()
    {
        using var algorithm = CreateAlgorithm();

        // Boundary sentinels that must always be invalid regardless of algorithm.
        foreach (int sentinel in new[] { int.MinValue, -1, 0, int.MaxValue })
        {
            Assert.IsFalse(
                algorithm.ValidTweakSize(sentinel),
                $"ValidTweakSize({sentinel}) should always return false.");
        }

        // Derive an invalid byte-aligned size from the algorithm's own legal ranges.
        int? invalidBits = FindInvalidTweakSize(algorithm.LegalTweakSizes);

        if (invalidBits is null)
        {
            Assert.Inconclusive(
                $"{typeof(TAlgorithm).Name} accepts every byte-aligned tweak length — " +
                "no invalid byte-aligned size can be constructed for this test.");
            return;
        }

        Assert.IsFalse(
            algorithm.ValidTweakSize(invalidBits.Value),
            $"ValidTweakSize({invalidBits.Value}) should return false for {typeof(TAlgorithm).Name}.");
    }

    /// <summary>
    /// Returns the first byte-aligned bit length that falls outside all ranges in
    /// <paramref name="legalSizes" />, or <see langword="null" /> when every byte-aligned length
    /// from 8 to 4096 bits is covered.
    /// </summary>
    private static int? FindInvalidTweakSize(KeySizes[] legalSizes)
    {
        for (int bytes = 1; bytes <= 512; bytes++)
        {
            int bits = bytes * 8;
            if (!IsLegalKeySize(bits, legalSizes))
                return bits;
        }

        return null;
    }

    private static bool IsLegalKeySize(int bits, KeySizes[] legalSizes)
    {
        foreach (var range in legalSizes)
        {
            if (bits < range.MinSize || bits > range.MaxSize)
                continue;

            if (range.SkipSize == 0)
                return bits == range.MinSize;

            if ((bits - range.MinSize) % range.SkipSize == 0)
                return true;
        }

        return false;
    }
}
