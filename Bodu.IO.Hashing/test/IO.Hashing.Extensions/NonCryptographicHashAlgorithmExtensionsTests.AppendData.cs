// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.AppendData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Hashing;
using Bodu.Test.IO;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for the synchronous <see cref="NonCryptographicHashAlgorithmExtensions.AppendData" /> overloads covering
/// span and stream inputs.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{
    // ─── Span overload — argument validation ──────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the span overload throws <see cref="ArgumentNullException" /> when the algorithm is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenAlgorithmIsNull_ForSpanOverload_ShouldThrowArgumentNullException()
    {
        NonCryptographicHashAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm!.AppendData(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }));
        });
    }

    // ─── Span overload — accumulation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty span does not alter the current hash state.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenSpanIsEmpty_ShouldNotContributeToHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        MonitoringNonCryptographicHashAlgorithm baseline = CreateAlgorithm();

        algorithm.AppendData(ReadOnlySpan<byte>.Empty);

        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that the supplied bytes contribute to the hash after retrieval.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenSpanContainsData_ShouldContributeToHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        byte[] expected = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));

        algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3, 4 }));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that multiple calls accumulate all supplied bytes correctly.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenCalledMultipleTimes_ShouldAccumulateAllBytes()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        byte[] expected = BitConverter.GetBytes((uint)(10 + 20 + 30 + 40));

        algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 10, 20 }));
        algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 30, 40 }));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.AppendData(NonCryptographicHashAlgorithm, ReadOnlySpan{byte})" />
    /// increments <see cref="MonitoringNonCryptographicHashAlgorithm.AppendCallCount" />, confirming the underlying
    /// <see cref="NonCryptographicHashAlgorithm.Append" /> path is exercised exactly once per call.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenCalled_ShouldInvokeAppendOnce()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        int before = algorithm.AppendCallCount;

        algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }));

        Assert.AreEqual(before + 1, algorithm.AppendCallCount);
    }

    // ─── Stream overload — argument validation ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the stream overload throws <see cref="ArgumentNullException" /> when the algorithm is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenAlgorithmIsNull_ForStreamOverload_ShouldThrowArgumentNullException()
    {
        NonCryptographicHashAlgorithm? algorithm = null;
        using MemoryStream stream = new(SampleData);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm!.AppendData(stream);
        });
    }

    /// <summary>
    /// Verifies that the stream overload throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.AppendData((Stream)null!);
        });
    }

    /// <summary>
    /// Verifies that a zero <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.AppendData(stream, bufferSize: 0);
        });
    }

    /// <summary>
    /// Verifies that a negative <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.AppendData(stream, bufferSize: -1);
        });
    }

    // ─── Stream overload — correctness ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty stream does not alter the current hash state.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenStreamIsEmpty_ShouldNotContributeToHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        MonitoringNonCryptographicHashAlgorithm baseline = CreateAlgorithm();

        algorithm.AppendData(new MemoryStream(Array.Empty<byte>()));

        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that stream bytes contribute to the hash identically to a direct span append.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenStreamHasData_ShouldMatchDirectSpanAppend()
    {
        MonitoringNonCryptographicHashAlgorithm fromStream = CreateAlgorithm();
        MonitoringNonCryptographicHashAlgorithm fromSpan = CreateAlgorithm();

        fromStream.AppendData(new MemoryStream(SampleData));
        fromSpan.AppendData(SampleData.AsSpan());

        CollectionAssert.AreEqual(fromSpan.GetCurrentHash(), fromStream.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that a small <paramref name="bufferSize" /> — forcing multiple read iterations — produces the same
    /// result as the default buffer size.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenBufferSizeSmallerThanInput_ShouldProduceCorrectHash()
    {
        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(SampleData.AsSpan());
        byte[] expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.AppendData(new MemoryStream(SampleData), bufferSize: 1);

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering data 1 byte at a time is correctly accumulated.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenSourceIsFixedChunkStream_ShouldProduceCorrectHash()
    {
        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(SampleData.AsSpan());
        byte[] expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.AppendData(new FixedChunkStream(SampleData, chunkSize: 1));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that an <see cref="IOException" /> thrown by a <see cref="FaultingStream" /> mid-read propagates
    /// cleanly out of the stream overload.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<IOException>(() =>
        {
            algorithm.AppendData(new FaultingStream(SampleData, throwAfterBytes: 2));
        });
    }
}
