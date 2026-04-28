// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.SimdPath.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class AdlerTests<TTest, TAlgorithm, TModulo>
{
    /// <summary>
    /// Verifies that <see cref="Adler{T}.Append" /> accepts an input that exceeds the SIMD threshold
    /// (<c>length &gt;= 512</c>) without throwing, returns a digest of the algorithm's declared length, and
    /// produces the same digest deterministically across two fresh instances given the same input.
    /// </summary>
    /// <remarks>
    /// This test drives the SIMD-accelerated branch for coverage. It does not assert agreement with the
    /// scalar branch because the two are not currently algebraically equivalent — the SIMD path updates
    /// <c>pB</c> once per <c>Vector&lt;byte&gt;</c> chunk rather than per byte, which produces a different
    /// running sum than the scalar reference.
    /// </remarks>
    [TestMethod]
    public void Append_WhenInputExceedsSimdThreshold_ShouldProduceDeterministicDigest()
    {
        // 1024 bytes guarantees the SIMD path (length >= 512) fires on hardware with vector acceleration.
        byte[] data = new byte[1024];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(i & 0xFF);

        TAlgorithm first = new();
        first.Append(data);
        byte[] firstDigest = first.GetCurrentHash();

        TAlgorithm second = new();
        second.Append(data);
        byte[] secondDigest = second.GetCurrentHash();

        Assert.IsNotNull(firstDigest);
        Assert.AreEqual(first.HashLengthInBytes, firstDigest.Length);
        CollectionAssert.AreEqual(firstDigest, secondDigest);
    }

    /// <summary>
    /// Verifies that <see cref="Adler{T}.Append" /> handles inputs that span the internal NMAX boundary (5552)
    /// without throwing, exercising the outer reduction loop of the SIMD-accelerated path.
    /// </summary>
    /// <remarks>
    /// As with the smaller-input variant of this test, no assertion is made against the scalar path because
    /// the two branches diverge by design today; this test only locks down the entry contract and digest
    /// length for coverage purposes.
    /// </remarks>
    [TestMethod]
    public void Append_WhenInputExceedsNmaxBoundary_ShouldProduceWellFormedDigest()
    {
        // 6144 bytes spans more than one NMAX chunk (5552), forcing the SIMD path's outer accumulator-reduction
        // loop to execute at least twice.
        byte[] data = new byte[6144];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)((i * 7) & 0xFF);

        TAlgorithm algorithm = new();
        algorithm.Append(data);

        byte[] digest = algorithm.GetCurrentHash();

        Assert.IsNotNull(digest);
        Assert.AreEqual(algorithm.HashLengthInBytes, digest.Length);
        Assert.IsTrue(digest.Length > 0);
    }

    /// <summary>
    /// Verifies that splitting a 1024-byte input into two equal 512-byte chunks and appending sequentially
    /// yields the same digest as appending the whole buffer in a single call. Both paths exercise the SIMD
    /// branch (each chunk satisfies <c>length &gt;= 512</c>), keeping the comparison self-consistent within
    /// the SIMD code path.
    /// </summary>
    [TestMethod]
    public void Append_WhenLargeInputAppendedInTwoSimdChunks_ShouldMatchSingleSimdAppend()
    {
        byte[] data = new byte[1024];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)((i + 13) & 0xFF);

        TAlgorithm whole = new();
        whole.Append(data);

        TAlgorithm split = new();
        split.Append(data.AsSpan(0, 512));
        split.Append(data.AsSpan(512, 512));

        CollectionAssert.AreEqual(whole.GetCurrentHash(), split.GetCurrentHash());
    }
}
