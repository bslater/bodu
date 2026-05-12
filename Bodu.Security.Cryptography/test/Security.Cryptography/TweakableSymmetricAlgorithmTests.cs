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
    /// Returns one row per tweak-size value (in bits) that <see cref="TweakableSymmetricAlgorithm.TweakSize" /> must
    /// reject. Values are derived from <see cref="TweakableSymmetricAlgorithmSpecification.DefaultTweakSizeBits" />
    /// and filtered to exclude any size that happens to be legal for the algorithm under test.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing an invalid tweak size in bits.</returns>
    public static IEnumerable<object[]> InvalidTweakSizeBitsData()
    {
        TweakableSymmetricAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legal = new(spec.LegalTweakSizesBits);
        int d = spec.DefaultTweakSizeBits;
        foreach (int candidate in new[] { 0, -1, d - 1, d + 1, d * 2, d / 2 })
        {
            if (!legal.Contains(candidate))
                yield return new object[] { candidate };
        }
    }

    /// <summary>
    /// Returns one row per tweak byte-array length that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" />
    /// and the matching decryptor must reject. Values are derived from
    /// <see cref="TweakableSymmetricAlgorithmSpecification.DefaultTweakSizeBits" /> and filtered to exclude any
    /// byte length that maps to a legal tweak size for the algorithm under test.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing an invalid tweak length in bytes.</returns>
    public static IEnumerable<object[]> InvalidTweakLengthBytesData()
    {
        TweakableSymmetricAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legalBytes = new(spec.LegalTweakSizesBits.Select(b => b / 8));
        int d = spec.DefaultTweakSizeBits / 8;
        foreach (int candidate in new[] { 0, 1, d - 1, d + 1, d * 2 })
        {
            if (!legalBytes.Contains(candidate))
                yield return new object[] { candidate };
        }
    }
}
