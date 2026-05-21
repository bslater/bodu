// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for symmetric algorithms to verify encryption, decryption, and property behaviours.
/// </summary>
/// <typeparam name="TAlgorithm">The type of symmetric algorithm under test.</typeparam>
[TestClass]
public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    : SymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Security.Cryptography.TweakableSymmetricAlgorithm
{
    /// <summary>
    /// Returns the <see cref="TweakableSymmetricAlgorithmSpecification" /> describing the expected observable
    /// properties of <typeparamref name="TAlgorithm" />, including tweak-related metadata.
    /// </summary>
    /// <returns>A <see cref="TweakableSymmetricAlgorithmSpecification" /> instance populated with the expected values.</returns>
    protected abstract override TweakableSymmetricAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Returns one row per key-size value, in bits, that <see cref="TweakableSymmetricAlgorithm.TweakSize" />
    /// must reject. Values are derived from each legal tweak size and filtered to exclude any size
    /// that happens to be legal for the algorithm under test.
    /// </summary>
    /// <returns>
    /// A sequence of single-element arrays, each containing an invalid tweak size in bits.
    /// </returns>
    public static IEnumerable<object[]> InvalidTweakSizeBitsData()
    {
        TweakableSymmetricAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legal = [.. spec.LegalTweakSizesBits];
        IEnumerable<int> candidates =
            new[] { -1, 0 }.Concat(
                legal.SelectMany(size => new[]
                {
                    size - 1,
                    size + 1,
                    size * 2,
                    size / 2
                }));

        foreach (var candidate in candidates.Distinct().OrderBy(size => size))
        {
            if (!legal.Contains(candidate))
                yield return new object[] { candidate };
        }
    }

    /// <summary>
    /// Returns one row per key-size value, in bytes, that <see cref="TweakableSymmetricAlgorithm.TweakSize" />
    /// must reject. Values are derived from each legal tweak size and filtered to exclude any size
    /// that happens to be legal for the algorithm under test.
    /// </summary>
    /// <returns>
    /// A sequence of single-element arrays, each containing an invalid tweak size in bytes.
    /// </returns>
    public static IEnumerable<object[]> InvalidTweakSizeBytesData()
    {
        TweakableSymmetricAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legal = [.. spec.LegalTweakSizesBits.Select(size => size / 8)];
        IEnumerable<int> candidates =
            new[] { -1, 0 }.Concat(
                legal.SelectMany(size => new[]
                {
                    size - 1,
                    size + 1,
                    size * 2,
                    size / 2
                }));

        foreach (var candidate in candidates.Distinct().OrderBy(size => size))
        {
            if (!legal.Contains(candidate))
                yield return new object[] { candidate };
        }
    }

}
