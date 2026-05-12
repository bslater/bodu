// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.NmaxBoundary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class AdlerTests<TTest, TAlgorithm, TModulo>
{
    private const int Nmax = 5552;

    /// <summary>
    /// Verifies that a single <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" />
    /// call carrying an input strictly larger than the internal <c>NMAX</c> reduction window produces the same
    /// digest as the per-byte canonical scalar recurrence — exercising the scalar-fallback modulo reduction at
    /// the <c>index % NMAX == 0</c> boundary on platforms where <see cref="System.Numerics.Vector.IsHardwareAccelerated" />
    /// is <see langword="false" />.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <remarks>
    /// <para>
    /// The scalar fallback's NMAX-boundary modulo reduction is otherwise unreachable on SIMD-accelerated CI: the
    /// SIMD branch consumes the entire input before the scalar tail loop is ever entered. This test still
    /// provides defensive correctness coverage on accelerated hardware, and exercises the targeted scalar branch
    /// on non-accelerated hardware.
    /// </para>
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Append_WhenSingleCallExceedsNmaxBoundary_ShouldMatchPerByteRecurrence(SingleTestVariant variant)
    {
        // Two NMAX windows plus one trailing byte: forces the scalar NMAX boundary to be crossed twice (at
        // index = NMAX and index = 2 * NMAX) when the test runs on a non-SIMD platform.
        var data = new byte[(2 * Nmax) + 1];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(i & 0xFF);

        NonCryptographicHashAlgorithm whole = CreateAlgorithm(variant);
        whole.Append(data);

        NonCryptographicHashAlgorithm perByte = CreateAlgorithm(variant);
        for (int i = 0; i < data.Length; i++)
            perByte.Append(data.AsSpan(i, 1));

        CollectionAssert.AreEqual(whole.GetCurrentHash(), perByte.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that a single <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" />
    /// call carrying an input of exactly <c>NMAX</c> bytes produces the same digest as the per-byte scalar
    /// recurrence — exercising the scalar-fallback modulo reduction at the precise <c>index == NMAX</c> point.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Append_WhenSingleCallEqualsNmaxBoundary_ShouldMatchPerByteRecurrence(SingleTestVariant variant)
    {
        var data = new byte[Nmax];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(i & 0xFF);

        NonCryptographicHashAlgorithm whole = CreateAlgorithm(variant);
        whole.Append(data);

        NonCryptographicHashAlgorithm perByte = CreateAlgorithm(variant);
        for (int i = 0; i < data.Length; i++)
            perByte.Append(data.AsSpan(i, 1));

        CollectionAssert.AreEqual(whole.GetCurrentHash(), perByte.GetCurrentHash());
    }
}
