// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.SimdParity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
        var data = AdlerSimdRegressionInputs.ModuloByte8K;

        NonCryptographicHashAlgorithm whole = CreateAlgorithm(variant);
        whole.Append(data);

        NonCryptographicHashAlgorithm perByte = CreateAlgorithm(variant);
        for (var i = 0; i < data.Length; i++)
            perByte.Append(data.AsSpan(i, 1));

        CollectionAssert.AreEqual(whole.GetCurrentHash(), perByte.GetCurrentHash());
    }

}

/// <summary>
/// Provides shared regression inputs used to pin Adler-style checksum behaviour against known canonical
/// (zlib-cross-checked) digests on the SIMD-accelerated path.
/// </summary>
/// <remarks>
/// Inputs are <c>(byte)(i &amp; 0xFF)</c> sequences sized to engage the SIMD branch (≥ 512 bytes). The
/// 1 KiB vector matches the reproduction in issue #127 — it crosses the SIMD threshold but stays inside a
/// single NMAX block. The 8 KiB vector additionally crosses the 5552-byte NMAX boundary so accumulator
/// reduction at the chunk join is exercised.
/// </remarks>
internal static class AdlerSimdRegressionInputs
{

    /// <summary>The 1024-byte input <c>0x00, 0x01, …, 0xFF, 0x00, …</c> (four repetitions of 0..255).</summary>
    public static readonly byte[] ModuloByte1K = Build(1024);

    /// <summary>The 8192-byte input <c>0x00, 0x01, …, 0xFF, 0x00, …</c> (thirty-two repetitions of 0..255).</summary>
    public static readonly byte[] ModuloByte8K = Build(8192);

    private static byte[] Build(int length)
    {
        var data = new byte[length];
        for (var i = 0; i < length; i++)
            data[i] = (byte)i;
        return data;
    }

}
