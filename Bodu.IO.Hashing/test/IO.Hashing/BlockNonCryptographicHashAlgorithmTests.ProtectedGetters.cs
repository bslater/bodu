// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.ProtectedGetters.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{
    /// <summary>
    /// Verifies that the protected <see cref="BlockNonCryptographicHashAlgorithm{T}.TotalLength" /> property
    /// returns zero before any input has been appended.
    /// </summary>
    [TestMethod]
    public void TotalLength_WhenNoInputAppended_ShouldReturnZero()
    {
        RecordingBlockHasher hasher = new();
        Assert.AreEqual(0UL, hasher.TotalLengthExposed);
    }

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.TotalLength" /> reports the cumulative
    /// number of bytes appended across multiple calls.
    /// </summary>
    [TestMethod]
    public void TotalLength_WhenInputAppended_ShouldEqualCumulativeByteCount()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 1, 2, 3 });
        hasher.Append(new byte[] { 4, 5 });

        Assert.AreEqual(5UL, hasher.TotalLengthExposed);
    }

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.ResidualByteCount" /> reports the number
    /// of bytes currently buffered but not yet flushed as a complete block.
    /// </summary>
    [TestMethod]
    public void ResidualByteCount_WhenInputIsBelowBlockSize_ShouldReportBufferedBytes()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01, 0x02 });

        Assert.AreEqual(2, hasher.ResidualByteCountExposed);
    }

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.ResidualByteCount" /> resets to zero once
    /// the residual plus new input combine to fill a complete block.
    /// </summary>
    [TestMethod]
    public void ResidualByteCount_WhenBlockIsFlushed_ShouldReturnZero()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        Assert.AreEqual(0, hasher.ResidualByteCountExposed);
    }

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.ResidualBytes" /> exposes a span over the
    /// currently buffered bytes whose contents match the trailing bytes of the most recent input.
    /// </summary>
    [TestMethod]
    public void ResidualBytes_WhenInputIsBuffered_ShouldExposeBufferedBytes()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0xAA, 0xBB, 0xCC });

        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, hasher.ResidualBytesExposed);
    }
}
