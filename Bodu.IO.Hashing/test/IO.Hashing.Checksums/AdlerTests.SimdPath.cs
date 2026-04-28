// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.SimdPath.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class AdlerTests<TTest, TAlgorithm, TModulo>
{
    /// <summary>
    /// Verifies that the SIMD-accelerated path in <see cref="Adler{T}.Append" /> (taken when the input is at
    /// least 512 bytes long) produces the same digest as the scalar fallback path that processes one byte at a
    /// time. This ensures the two implementations remain in agreement regardless of whether
    /// <see cref="Vector.IsHardwareAccelerated" /> is enabled on the host.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputExceedsSimdThreshold_ShouldMatchScalarDigest()
    {
        // 1024 bytes guarantees the SIMD path (length >= 512) fires; the scalar reference is computed via
        // single-byte appends, which only exercises the trailing scalar loop.
        byte[] data = new byte[1024];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(i & 0xFF);

        TAlgorithm bulk = new();
        bulk.Append(data);

        TAlgorithm perByte = new();
        for (int i = 0; i < data.Length; i++)
            perByte.Append(new[] { data[i] });

        CollectionAssert.AreEqual(bulk.GetCurrentHash(), perByte.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that the SIMD-accelerated path correctly performs modular reduction for inputs longer than the
    /// internal NMAX constant (5552), producing a digest equal to the scalar reference.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputExceedsNmaxBoundary_ShouldMatchScalarDigest()
    {
        // 6144 bytes spans more than one NMAX chunk (5552), forcing the SIMD path's outer accumulator-reduction
        // loop to execute at least twice while the scalar reference uses only the per-byte path.
        byte[] data = new byte[6144];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)((i * 7) & 0xFF);

        TAlgorithm bulk = new();
        bulk.Append(data);

        TAlgorithm perByte = new();
        for (int i = 0; i < data.Length; i++)
            perByte.Append(new[] { data[i] });

        CollectionAssert.AreEqual(bulk.GetCurrentHash(), perByte.GetCurrentHash());
    }
}
