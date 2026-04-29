// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.SimdParity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class AdlerTests<TTest, TAlgorithm, TModulo>
{
    /// <summary>
    /// Verifies that hashing an input long enough to engage the SIMD-accelerated branch
    /// (≥ 512 bytes and crossing the internal NMAX reduction window) produces the same digest
    /// as appending the same bytes one at a time, which always exercises the canonical scalar
    /// per-byte recurrence.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <remarks>
    /// Earlier known-answer vectors (<c>Empty</c>, <c>Abc</c>, <c>QuickBrownFox</c>, <c>Zeros16</c>,
    /// <c>Sequential0To255</c>) are all under 512 bytes and never trigger the SIMD branch, so a
    /// divergence between the two paths could previously go undetected. This vector is sized to
    /// straddle the 5552-byte NMAX boundary and exercise multiple full vector chunks plus a
    /// scalar tail.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Append_WhenInputEngagesSimdBranch_ShouldMatchPerByteScalarPath(SingleTestVariant variant)
    {
        byte[] data = new byte[8192];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)i;

        NonCryptographicHashAlgorithm whole = CreateAlgorithm(variant);
        whole.Append(data);

        NonCryptographicHashAlgorithm perByte = CreateAlgorithm(variant);
        for (int i = 0; i < data.Length; i++)
            perByte.Append(data.AsSpan(i, 1));

        CollectionAssert.AreEqual(whole.GetCurrentHash(), perByte.GetCurrentHash());
    }
}
