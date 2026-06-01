// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.ComputeHashAsync.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Security.Cryptography;
using Bodu.Test;
using Bodu.Test.IO;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that computing the hash over various long stream sizes works consistently.
    /// </summary>
    /// <param name="length">The length of the test input stream.</param>
    [TestMethod]
    [DataRow(1_000)]
    [DataRow(100_000)]
    [DataRow(1_000_000)]
    [DataRow(10_000_000)]
    public async Task ComputeHashAsync_LongStream_WithVariousLengths_ShouldSucceed(int length)
    {
        using var stream = new FixedLengthIncrementingStream(length);
        using TAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.ComputeHashAsync(stream);

        Assert.IsNotNull(result);
        Assert.AreEqual(algorithm.HashSize / 8, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHashAsync" /> supports cancellation during slow stream processing.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCancelled_ShouldThrowExactly()
    {
        using var cancellationSource = new CancellationTokenSource(1000); // cancel after 1 second
        using var stream = new ThrottledIncrementingByteStream(10000); // delayed stream that is 10 seconds
        using TAlgorithm algorithm = CreateAlgorithm();

        await Task.WhenAny(
            Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            {
                return algorithm.ComputeHashAsync(stream, cancellationSource.Token);
            }),
            Task.Delay(5000));   // just in case
    }

    /// <summary>
    /// Verifies behaviour when <see cref="HashAlgorithmExtensions.ComputeHashAsync" /> is cancelled before the await begins.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCancelledBeforeAwait_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var stream = new FixedLengthIncrementingStream(int.MaxValue);

        // create a canellation token and cancel it before computation starts
        using var cancellationTokenSource = new CancellationTokenSource();
#if NET6_0_OR_GREATER
        await cancellationTokenSource.CancelAsync();
#else
        cancellationTokenSource.Cancel();
#endif
        Task<byte[]> task = algorithm.ComputeHashAsync(stream, cancellationTokenSource.Token);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => task);

        Assert.IsTrue(task.IsCanceled, "Task should be marked as canceled.");
        Assert.AreEqual(0, stream.Position, "Stream position should not have advanced.");
        Assert.IsNull(algorithm.Hash, "Hash hashValue should be null.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.ComputeHashAsync" /> throws TaskCanceledException if the cancellation token is already canceled.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCancelledTokenIsPassed_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(new byte[4096]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await algorithm.ComputeHashAsync(stream, cts.Token);
        });
    }

    /// <summary>
    /// Verifies that computing the hash on the same long input produces the same result each time.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenRepeatedOnSameInput_ShouldProduceSameHash()
    {
        const int length = 1 * 1024 * 1024; // 1 MB
        using var streamA = new FixedLengthIncrementingStream(length);
        using var streamB = new FixedLengthIncrementingStream(length);
        using TAlgorithm algorithmA = CreateAlgorithm();
        using TAlgorithm algorithmB = CreateAlgorithm();

        var hashA = await algorithmA.ComputeHashAsync(streamA);
        var hashB = await algorithmB.ComputeHashAsync(streamB);

        CollectionAssert.AreEqual(hashA, hashB, "Hashes from identical input should match.");
    }

    /// <summary>
    /// Verifies that hashing an empty stream produces the expected result.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsEmpty_ShouldReturnExpectedHash()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream([]);

        var actual = await algorithm.ComputeHashAsync(stream);

        CollectionAssert.AreEqual(ExpectedEmptyInputHash, actual);
    }

    /// <summary>
    /// Verifies that all bytes in a long stream are processed when computing the hash. This is only applicable to algorithms that
    /// expose the <c>BytesProcessed</c> property.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsLong_ShouldProcessAllBytes()
    {
        const int length = 1 * 1024 * 1024; // 1 MB
        using var stream = new FixedLengthIncrementingStream(length);
        using TAlgorithm algorithm = CreateAlgorithm();

        await algorithm.ComputeHashAsync(stream);

        Assert.AreEqual(length, stream.Position, "All bytes should have been processed.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.ComputeHashAsync" /> throws ArgumentNullException when the stream is null.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsNull_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.ComputeHashAsync(null!);
        });
    }

    /// <summary>
    /// Verifies that the asynchronous
    /// <see cref="HashAlgorithm.ComputeHashAsync(Stream, System.Threading.CancellationToken)" />
    /// path produces the correct hash value for incrementally growing inputs from empty input
    /// through one byte past the spec's coverage threshold.
    /// </summary>
    /// <param name="variant">The variant identifier supplied by the dynamic data source.</param>
    /// <returns>A task that completes when all incremental lengths have been verified.</returns>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public Task ComputeHashAsync_WhenUsingIncrementalInput_ShouldMatchExpected(TVariant variant) =>
        AssertIncrementalInputAsync(variant, async (algorithm, input, byteCount) =>
        {
            await using var stream = new MemoryStream(input, 0, byteCount, writable: false);
            return await algorithm.ComputeHashAsync(stream).ConfigureAwait(false);
        });

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHashAsync" />, when UsingNamedInput, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ComputeHashNamedInputTestData))]
    public async Task ComputeHashAsync_WhenUsingNamedInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected)
    {
        using TAlgorithm algorithm = CreateAlgorithm(variant);
        await using var stream = new MemoryStream(input);

        var actual = await algorithm.ComputeHashAsync(stream);

        TestHelpers.TraceWriteIfNotEqual(expected, actual);

        CollectionAssert.AreEqual(expected, actual, $"Hash mismatch for {testName} using variant '{variant}'.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHashAsync" /> correctly interacts with a stream by tracking all read operations
    /// using <see cref="MonitoringStream" />. Ensures the total number of bytes read matches the expected length, and that multiple
    /// reads were issued (i.e., streaming behaviour).
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WithMonitoringStream_ShouldTrackAllReads()
    {
        const int length = 2 * 1024 * 1024; // 2 MB

        // Wrap a deterministic input stream with a monitoring stream to capture read behavior
        var baseStream = new FixedLengthIncrementingStream(length);
        var monitoredStream = new MonitoringStream(baseStream);

        // Compute the hash asynchronously
        using TAlgorithm algorithm = CreateAlgorithm();
        await algorithm.ComputeHashAsync(monitoredStream);

        // Verify the total number of bytes read matches the expected input length
        Assert.AreEqual(length, monitoredStream.Reads.Sum(r => r.Count), "Total bytes read should match stream length.");

        // Confirm that the algorithm read in multiple chunks (not one large buffer)
        Assert.IsTrue(monitoredStream.Reads.Count > 1, "Expected multiple read operations.");

        // Output captured read info for debugging/analysis
        Trace.WriteLine($"Read Count: {monitoredStream.Reads.Count}");
        Trace.WriteLine($"First Read: {monitoredStream.Reads.FirstOrDefault()}");
        Trace.WriteLine($"Last Read : {monitoredStream.Reads.LastOrDefault()}");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHashAsync(Stream, CancellationToken)" /> throws an
    /// <see cref="ObjectDisposedException" /> before reading from the supplied stream when the algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void ComputeHashAsync_WhenAlgorithmDisposed_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        using var stream = new ThrowOnReadStream();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.ComputeHashAsync(stream);
        });

        Assert.IsFalse(stream.WasRead);
    }
}
