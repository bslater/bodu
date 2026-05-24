// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.ComputeHash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using Bodu.Test.IO;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for the synchronous <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHash(NonCryptographicHashAlgorithm, byte[])" />,
/// <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHash(NonCryptographicHashAlgorithm, byte[], int, int)" />,
/// <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHash(NonCryptographicHashAlgorithm, ReadOnlySpan{byte})" />, and
/// <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHash(NonCryptographicHashAlgorithm, Stream, int)" /> overloads.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{

    /// <summary>Hash bytes corresponding to a sum of zero (i.e. the digest of empty input).</summary>
    private static readonly byte[] s_emptyHash = BitConverter.GetBytes((uint)0);

    /// <summary>
    /// Verifies that the byte-array overload incorporates any state previously accumulated on the algorithm
    /// before computing the digest.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenAlgorithmHasPriorState_ForByteArrayOverload_ShouldIncludePriorBytesInHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.AppendData(new ReadOnlySpan<byte>([100]));
        var expected = BitConverter.GetBytes((uint)(100 + 1 + 2 + 3 + 4));

        var result = algorithm.ComputeHash(s_sampleData);

        CollectionAssert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that the stream overload incorporates any state previously accumulated on the algorithm before
    /// reading from the stream.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenAlgorithmHasPriorState_ForStreamOverload_ShouldIncludePriorBytesInHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.AppendData(new ReadOnlySpan<byte>([50]));
        var expected = BitConverter.GetBytes((uint)(50 + 1 + 2 + 3 + 4));

        var result = algorithm.ComputeHash(new MemoryStream(s_sampleData));

        CollectionAssert.AreEqual(expected, result);
    }

    // ─── byte[] overload — argument validation ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the byte-array overload throws <see cref="ArgumentNullException" /> when the algorithm is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenAlgorithmIsNull_ForByteArrayOverload_ShouldThrowExactly()
    {
        NonCryptographicHashAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm!.ComputeHash(s_sampleData);
        });
    }

    // ─── byte[] + offset + count overload — argument validation ───────────────────────────────

    /// <summary>
    /// Verifies that the byte-array range overload throws <see cref="ArgumentNullException" /> when the algorithm is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenAlgorithmIsNull_ForByteArrayRangeOverload_ShouldThrowExactly()
    {
        NonCryptographicHashAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm!.ComputeHash(s_sampleData, 0, s_sampleData.Length);
        });
    }

    // ─── ReadOnlySpan<byte> overload — argument validation ────────────────────────────────────

    /// <summary>
    /// Verifies that the span overload throws <see cref="ArgumentNullException" /> when the algorithm is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenAlgorithmIsNull_ForSpanOverload_ShouldThrowExactly()
    {
        NonCryptographicHashAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm!.ComputeHash(new ReadOnlySpan<byte>(s_sampleData));
        });
    }

    // ─── Stream overload — argument validation ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the stream overload throws <see cref="ArgumentNullException" /> when the algorithm is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenAlgorithmIsNull_ForStreamOverload_ShouldThrowExactly()
    {
        NonCryptographicHashAlgorithm? algorithm = null;
        using MemoryStream stream = new(s_sampleData);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm!.ComputeHash(stream);
        });
    }

    /// <summary>
    /// Verifies that the byte-array overload returns the digest of all supplied bytes.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBufferContainsData_ForByteArrayOverload_ShouldReturnExpectedHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash(s_sampleData);

        CollectionAssert.AreEqual(s_sampleHash, result);
    }

    // ─── byte[] overload — correctness ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty buffer produces the digest corresponding to a zero accumulator.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBufferIsEmpty_ForByteArrayOverload_ShouldReturnEmptyHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash([]);

        CollectionAssert.AreEqual(s_emptyHash, result);
    }

    /// <summary>
    /// Verifies that the byte-array overload throws <see cref="ArgumentNullException" /> when the buffer is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBufferIsNull_ForByteArrayOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.ComputeHash((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that the byte-array range overload throws <see cref="ArgumentNullException" /> when the buffer is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBufferIsNull_ForByteArrayRangeOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.ComputeHash((byte[])null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that a negative <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBufferSizeIsNegative_ForStreamOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = algorithm.ComputeHash(stream, bufferSize: -1);
        });
    }

    /// <summary>
    /// Verifies that a zero <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBufferSizeIsZero_ForStreamOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = algorithm.ComputeHash(stream, bufferSize: 0);
        });
    }

    /// <summary>
    /// Verifies that a small <paramref name="bufferSize" /> — forcing multiple read iterations — produces the same
    /// digest as the default buffer size.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBufferSizeSmallerThanInput_ForStreamOverload_ShouldReturnExpectedHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash(new MemoryStream(s_sampleData), bufferSize: 1);

        CollectionAssert.AreEqual(s_sampleHash, result);
    }

    /// <summary>
    /// Verifies that the byte-array overload increments
    /// <see cref="MonitoringNonCryptographicHashAlgorithm.ResetCallCount" />, confirming the underlying
    /// <c>GetHashAndReset</c> path is exercised.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalled_ForByteArrayOverload_ShouldResetAlgorithm()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var before = algorithm.ResetCallCount;

        _ = algorithm.ComputeHash(s_sampleData);

        Assert.AreEqual(before + 1, algorithm.ResetCallCount);
    }

    /// <summary>
    /// Verifies that the byte-array overload resets the underlying algorithm state, so two successive computations
    /// over different inputs return independent digests.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledTwice_ForByteArrayOverload_ShouldResetStateBetweenCalls()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var first = algorithm.ComputeHash(s_sampleData);
        var second = algorithm.ComputeHash([5, 6]);
        var expectedSecond = BitConverter.GetBytes((uint)(5 + 6));

        CollectionAssert.AreEqual(s_sampleHash, first);
        CollectionAssert.AreEqual(expectedSecond, second);
    }

    /// <summary>
    /// Verifies that the span overload resets the algorithm so a subsequent call begins from a clean state.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledTwice_ForSpanOverload_ShouldResetStateBetweenCalls()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var first = algorithm.ComputeHash(new ReadOnlySpan<byte>(s_sampleData));
        var second = algorithm.ComputeHash(new ReadOnlySpan<byte>([7, 8]));
        var expectedSecond = BitConverter.GetBytes((uint)(7 + 8));

        CollectionAssert.AreEqual(s_sampleHash, first);
        CollectionAssert.AreEqual(expectedSecond, second);
    }

    /// <summary>
    /// Verifies that the stream overload resets the algorithm so a subsequent call begins from a clean state.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledTwice_ForStreamOverload_ShouldResetStateBetweenCalls()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var first = algorithm.ComputeHash(new MemoryStream(s_sampleData));
        var second = algorithm.ComputeHash(new MemoryStream([9, 10]));
        var expectedSecond = BitConverter.GetBytes((uint)(9 + 10));

        CollectionAssert.AreEqual(s_sampleHash, first);
        CollectionAssert.AreEqual(expectedSecond, second);
    }

    /// <summary>
    /// Verifies that a negative <paramref name="count" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCountIsNegative_ForByteArrayRangeOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = algorithm.ComputeHash(s_sampleData, 0, -1);
        });
    }

    // ─── byte[] + offset + count overload — correctness ───────────────────────────────────────

    /// <summary>
    /// Verifies that a zero <paramref name="count" /> returns the digest of empty input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCountIsZero_ForByteArrayRangeOverload_ShouldReturnEmptyHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash(s_sampleData, 0, 0);

        CollectionAssert.AreEqual(s_emptyHash, result);
    }

    /// <summary>
    /// Verifies that an <paramref name="offset" /> exceeding the buffer length throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetExceedsBuffer_ForByteArrayRangeOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = algorithm.ComputeHash(s_sampleData, s_sampleData.Length + 1, 0);
        });
    }

    /// <summary>
    /// Verifies that a negative <paramref name="offset" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetIsNegative_ForByteArrayRangeOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = algorithm.ComputeHash(s_sampleData, -1, 1);
        });
    }

    /// <summary>
    /// Verifies that an <paramref name="offset" /> and <paramref name="count" /> combination extending past the end
    /// of the buffer throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetPlusCountExceedsBuffer_ForByteArrayRangeOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = algorithm.ComputeHash(s_sampleData, 2, s_sampleData.Length);
        });
    }

    /// <summary>
    /// Verifies that hashing the full buffer via the range overload is equivalent to the byte-array overload.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenRangeIsFullBuffer_ForByteArrayRangeOverload_ShouldMatchByteArrayOverload()
    {
        MonitoringNonCryptographicHashAlgorithm rangeAlgorithm = CreateAlgorithm();
        MonitoringNonCryptographicHashAlgorithm fullAlgorithm = CreateAlgorithm();

        var viaRange = rangeAlgorithm.ComputeHash(s_sampleData, 0, s_sampleData.Length);
        var viaFull = fullAlgorithm.ComputeHash(s_sampleData);

        CollectionAssert.AreEqual(viaFull, viaRange);
    }

    /// <summary>
    /// Verifies that the range overload hashes only the bytes within the requested window.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenRangeIsSubset_ForByteArrayRangeOverload_ShouldReturnHashOfSubset()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var expected = BitConverter.GetBytes((uint)(2 + 3));

        var result = algorithm.ComputeHash(s_sampleData, 1, 2);

        CollectionAssert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that an <see cref="IOException" /> thrown by a <see cref="FaultingStream" /> mid-read propagates
    /// cleanly out of the stream overload.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSourceFaultsMidRead_ForStreamOverload_ShouldPropagateIOException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<IOException>(() =>
        {
            _ = algorithm.ComputeHash(new FaultingStream(s_sampleData, throwAfterBytes: 2));
        });
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering data 1 byte at a time produces the expected
    /// digest.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSourceIsFixedChunkStream_ForStreamOverload_ShouldReturnExpectedHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash(new FixedChunkStream(s_sampleData, chunkSize: 1));

        CollectionAssert.AreEqual(s_sampleHash, result);
    }

    /// <summary>
    /// Verifies that the span overload returns the digest of all supplied bytes.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSpanContainsData_ForSpanOverload_ShouldReturnExpectedHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash(new ReadOnlySpan<byte>(s_sampleData));

        CollectionAssert.AreEqual(s_sampleHash, result);
    }

    // ─── ReadOnlySpan<byte> overload — correctness ────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty span produces the digest corresponding to a zero accumulator.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSpanIsEmpty_ForSpanOverload_ShouldReturnEmptyHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash([]);

        CollectionAssert.AreEqual(s_emptyHash, result);
    }

    /// <summary>
    /// Verifies that the stream overload returns the digest of all bytes read from the source.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamHasData_ForStreamOverload_ShouldMatchSpanOverload()
    {
        MonitoringNonCryptographicHashAlgorithm fromStream = CreateAlgorithm();
        MonitoringNonCryptographicHashAlgorithm fromSpan = CreateAlgorithm();

        var viaStream = fromStream.ComputeHash(new MemoryStream(s_sampleData));
        var viaSpan = fromSpan.ComputeHash(new ReadOnlySpan<byte>(s_sampleData));

        CollectionAssert.AreEqual(viaSpan, viaStream);
    }

    // ─── Stream overload — correctness ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty stream produces the digest corresponding to a zero accumulator.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamIsEmpty_ForStreamOverload_ShouldReturnEmptyHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.ComputeHash(new MemoryStream([]));

        CollectionAssert.AreEqual(s_emptyHash, result);
    }

    /// <summary>
    /// Verifies that the stream overload throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamIsNull_ForStreamOverload_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.ComputeHash((Stream)null!);
        });
    }

}
