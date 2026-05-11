// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.VerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for <see cref="HashAlgorithmExtensions.VerifyHashAsync" /> covering expected-hash
/// variants (byte array, hex, memory), stream-shape behaviours, and error propagation.
/// </summary>
/// <remarks>
/// <c>VerifyHashAsync</c> propagates all exceptions — <see cref="IOException" />,
/// <see cref="OperationCanceledException" />, and null-argument errors — to the caller.
/// Swallow-and-return-<see langword="false" /> behaviour belongs to <c>TryVerifyHashAsync</c>.
/// </remarks>
public partial class HashAlgorithmExtensionsTests
{
    // ─── Expected-hash variants — matching input ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that a stream whose content matches the expected hex string returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHex));
    }

    /// <summary>
    /// Verifies that a stream whose content matches the expected byte array returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHash));
    }

    /// <summary>
    /// Verifies that a stream whose content matches the expected <see cref="ReadOnlyMemory{T}" />
    /// returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        ReadOnlyMemory<byte> expected = SampleHash;
        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, expected));
    }

    // ─── Mismatch and malformed-input edge cases ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that mismatched hash values produce <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenHashDoesNotMatch_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        var badHash = BitConverter.GetBytes((uint)9999);
        Assert.IsFalse(await algorithm.VerifyHashAsync(stream, badHash));
    }

    /// <summary>
    /// Verifies that a malformed hex string short-circuits the method and returns
    /// <see langword="false" /> without reading the source stream.
    /// </summary>
    /// <remarks>
    /// Parsing the hex expected value is a prerequisite to computing the stream hash. If parsing
    /// fails, the stream should never be touched — this test asserts that via
    /// <see cref="MonitoringStream.Reads" />.
    /// </remarks>
    [TestMethod]
    public async Task VerifyHashAsync_WhenHexIsMalformed_ShouldReturnFalseWithoutReadingStream()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var baseStream = new MemoryStream(SampleData);
        using var monitored = new MonitoringStream(baseStream);

        var result = await algorithm.VerifyHashAsync(monitored, "ZZZZ");

        Assert.IsFalse(result);
        Assert.AreEqual(0, monitored.Reads.Count);
    }

    // ─── Stream-shape coverage ────────────────────────────────────────────────────────────────
    // The following tests exercise the read-accumulation loop against every test-infrastructure
    // stream shape: chunked delivery, non-seekable, throttled, partial-read, and monitored. A
    // hash is determined solely by byte content, so every shape must yield the same digest.

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering data 1 byte at a time produces
    /// the correct hash, confirming the accumulation path handles minimum chunk granularity.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceIsFixedChunkStream_OneBytePerRead_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new FixedChunkStream(SampleData, chunkSize: 1);

        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHash),
            "VerifyHashAsync must return true for SampleData delivered 1 byte at a time.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> with a chunk size not aligned to the
    /// algorithm's internal block size still produces the correct hash.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceIsFixedChunkStream_NonBlockAlignedChunk_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new FixedChunkStream(SampleData, chunkSize: 3);

        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHash),
            "VerifyHashAsync must return true for SampleData delivered in 3-byte chunks.");
    }

    /// <summary>
    /// Verifies that <see cref="IncrementingByteStream" /> — which returns at most half of its
    /// remaining bytes per read — produces the correct hash, exercising the accumulation loop
    /// under guaranteed partial reads.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceIsIncrementingByteStream_ShouldReturnTrue()
    {
        const int length = 64;

        byte[] expectedHash;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsTrue(await algorithm.VerifyHashAsync(new IncrementingByteStream(length), expectedHash),
            "VerifyHashAsync must return true for an IncrementingByteStream producing the expected byte sequence.");
    }

    /// <summary>
    /// Verifies that a <see cref="NonSeekableStream" /> produces the correct hash, confirming
    /// the method never invokes seek-related members on its source.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceIsNonSeekable_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new NonSeekableStream(SampleData);

        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHash),
            "VerifyHashAsync must return true for SampleData read from a NonSeekableStream.");
    }

    /// <summary>
    /// Verifies that a <see cref="ThrottledIncrementingByteStream" /> — which delays each read
    /// to simulate slow I/O — produces the same hash as direct computation.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceIsThrottledIncrementingByteStream_ShouldReturnTrue()
    {
        const int length = 64;

        // Compute the expected hash from the equivalent sequential byte sequence using ToArray,
        // which generates the data independently without incurring the read delay.
        byte[] expectedHash;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new ThrottledIncrementingByteStream(length, readDelay: 5);

        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, expectedHash),
            "VerifyHashAsync must return true for a ThrottledIncrementingByteStream producing the expected byte sequence.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedLengthIncrementingStream" /> — which delivers sequential
    /// bytes in partial reads — produces the correct hash.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceIsFixedLengthIncrementingStream_ShouldReturnTrue()
    {
        const int length = 64;

        byte[] expectedHash;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new FixedLengthIncrementingStream(length);

        Assert.IsTrue(await algorithm.VerifyHashAsync(stream, expectedHash),
            "VerifyHashAsync must return true for a FixedLengthIncrementingStream producing the expected byte sequence.");
    }

    /// <summary>
    /// Verifies that a <see cref="MonitoringStream" /> is actually read during hash computation
    /// and that the method returns <see langword="true" /> when the hash matches.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenUsingMonitoringStream_ShouldTrackReadsAndReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var baseStream = new MemoryStream(new byte[] { 2, 3 });
        using var monitored = new MonitoringStream(baseStream);
        var expected = BitConverter.GetBytes((uint)5); // additive hash of { 2, 3 }

        var result = await algorithm.VerifyHashAsync(monitored, expected);

        Assert.IsTrue(result);
        Assert.IsTrue(monitored.Reads.Count > 0);
    }

    // ─── Error propagation ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an <see cref="IOException" /> raised mid-read by a
    /// <see cref="FaultingStream" /> propagates out of the method unmodified.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        // Fault after 1 byte — well before EOF — to confirm the exception is not swallowed.
        using var stream = new FaultingStream(SampleData, throwAfterBytes: 1);

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await algorithm.VerifyHashAsync(stream, SampleHash),
            "VerifyHashAsync must propagate IOException from a faulting stream.");
    }

    /// <summary>
    /// Verifies that a <see cref="CancellationTriggerStream" /> cancelling the token mid-stream
    /// causes the method to throw <see cref="OperationCanceledException" /> (or its async
    /// subtype <see cref="TaskCanceledException" />).
    /// </summary>
    /// <remarks>
    /// <see cref="IncrementingByteStream" /> is used as the inner stream because it returns at
    /// most half of its remaining bytes per read, guaranteeing multiple reads before EOF. A plain
    /// <see cref="MemoryStream" /> may deliver all bytes in a single call, allowing the stream to
    /// complete before the cancellation trigger fires.
    /// </remarks>
    [TestMethod]
    public async Task VerifyHashAsync_WhenCancellationTriggeredMidStream_ShouldThrowOperationCanceledException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerStream(
            new IncrementingByteStream(1024), cts, cancelAfterRead: 2);

        // ThrowsAsync (not ThrowsExactlyAsync) because async I/O surfaces the cancellation as
        // TaskCanceledException, a subtype of OperationCanceledException.
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await algorithm.VerifyHashAsync(stream, new byte[4], cts.Token),
            "VerifyHashAsync must throw OperationCanceledException when the token is cancelled mid-stream.");
    }

    /// <summary>
    /// Verifies that a cancellation token already in the cancelled state causes the method to
    /// throw without attempting any read.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenTokenAlreadyCancelled_ShouldThrowOperationCanceledException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await algorithm.VerifyHashAsync(stream, SampleHash, cts.Token);
        });
    }

    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a null algorithm receiver raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        HashAlgorithm? algorithm = null;
        using var stream = new MemoryStream(SampleData);
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm!.VerifyHashAsync(stream, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null stream raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.VerifyHashAsync(null!, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected byte-array hash raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenExpectedHashIsNull_ShouldThrowArgumentNullException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.VerifyHashAsync(stream, (byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that a null expected hex string raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenExpectedHexIsNull_ShouldThrowArgumentNullException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.VerifyHashAsync(stream, (string)null!);
        });
    }
}
