// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.TryVerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for <see cref="HashAlgorithmExtensions.TryVerifyHashAsync" /> covering matching-input
/// variants, stream-shape behaviours, and graceful handling of stream faults and cancellation.
/// </summary>
/// <remarks>
/// <para>
/// The try-pattern swallows all runtime failures — <see cref="IOException" />,
/// <see cref="OperationCanceledException" />, format errors — and signals them via
/// <see langword="false" /> rather than propagating.
/// </para>
/// <para>
/// The byte-array and string overloads still raise <see cref="ArgumentNullException" /> for
/// null inputs, encoding, or expected hash, because those represent programmer errors. The
/// stream overload is more forgiving and returns <see langword="false" /> for a null stream or
/// null expected hash.
/// </para>
/// </remarks>
public partial class HashAlgorithmExtensionsTests
{
    // ─── Matching input → true ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayMatches_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHash);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when the
    /// expected hash is supplied as a hex string.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayMatchesHex_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHex);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching string + encoding input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenEncodedStringMatches_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        bool result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, SampleStringHash);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected
    /// hash is supplied as a byte array.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        bool result = await algorithm.TryVerifyHashAsync(stream, SampleHash);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected
    /// hash is supplied as a hex string.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        bool result = await algorithm.TryVerifyHashAsync(stream, SampleHex);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected
    /// hash is supplied as a <see cref="ReadOnlyMemory{T}" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        ReadOnlyMemory<byte> expected = SampleHash;
        bool result = await algorithm.TryVerifyHashAsync(stream, expected);
        Assert.IsTrue(result);
    }

    // ─── Stream-shape coverage ────────────────────────────────────────────────────────────────
    // Mirrors the stream-shape coverage of VerifyHashAsync to confirm the try-pattern reads the
    // stream identically — the only behavioural difference is exception swallowing, exercised
    // in the error-handling section below.

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering data 1 byte at a time returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceIsFixedChunkStream_OneBytePerRead_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new FixedChunkStream(SampleData, chunkSize: 1);

        Assert.IsTrue(await algorithm.TryVerifyHashAsync(stream, SampleHash),
            "TryVerifyHashAsync must return true for SampleData delivered 1 byte at a time.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> with a non-block-aligned chunk size
    /// returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceIsFixedChunkStream_NonBlockAlignedChunk_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new FixedChunkStream(SampleData, chunkSize: 3);

        Assert.IsTrue(await algorithm.TryVerifyHashAsync(stream, SampleHash),
            "TryVerifyHashAsync must return true for SampleData delivered in 3-byte chunks.");
    }

    /// <summary>
    /// Verifies that an <see cref="IncrementingByteStream" /> produces the correct hash and
    /// returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceIsIncrementingByteStream_ShouldReturnTrue()
    {
        const int length = 64;

        byte[] expectedHash;
        using (var reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using var algorithm = CreateAlgorithm();

        Assert.IsTrue(await algorithm.TryVerifyHashAsync(new IncrementingByteStream(length), expectedHash),
            "TryVerifyHashAsync must return true for an IncrementingByteStream producing the expected byte sequence.");
    }

    /// <summary>
    /// Verifies that a <see cref="NonSeekableStream" /> is accepted and returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceIsNonSeekable_ShouldReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new NonSeekableStream(SampleData);

        Assert.IsTrue(await algorithm.TryVerifyHashAsync(stream, SampleHash),
            "TryVerifyHashAsync must return true for SampleData read from a NonSeekableStream.");
    }

    /// <summary>
    /// Verifies that a <see cref="ThrottledIncrementingByteStream" /> returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceIsThrottledIncrementingByteStream_ShouldReturnTrue()
    {
        const int length = 64;

        byte[] expectedHash;
        using (var reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using var algorithm = CreateAlgorithm();
        using var stream = new ThrottledIncrementingByteStream(length, readDelay: 5);

        Assert.IsTrue(await algorithm.TryVerifyHashAsync(stream, expectedHash),
            "TryVerifyHashAsync must return true for a ThrottledIncrementingByteStream producing the expected byte sequence.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedLengthIncrementingStream" /> returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceIsFixedLengthIncrementingStream_ShouldReturnTrue()
    {
        const int length = 64;

        byte[] expectedHash;
        using (var reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using var algorithm = CreateAlgorithm();
        using var stream = new FixedLengthIncrementingStream(length);

        Assert.IsTrue(await algorithm.TryVerifyHashAsync(stream, expectedHash),
            "TryVerifyHashAsync must return true for a FixedLengthIncrementingStream producing the expected byte sequence.");
    }

    /// <summary>
    /// Verifies that <see cref="MonitoringStream" /> is actually read during hash computation —
    /// the try-pattern must not short-circuit stream access.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenUsingMonitoringStream_ShouldTrackReadsAndReturnTrue()
    {
        using var algorithm = CreateAlgorithm();
        using var baseStream = new MemoryStream(new byte[] { 2, 3 });
        using var monitored = new MonitoringStream(baseStream);
        byte[] expected = BitConverter.GetBytes((uint)5); // additive hash of { 2, 3 }

        bool result = await algorithm.TryVerifyHashAsync(monitored, expected);

        Assert.IsTrue(result,
            "TryVerifyHashAsync must return true when the stream content matches the expected hash.");
        Assert.IsTrue(monitored.Reads.Count > 0,
            "TryVerifyHashAsync must actually read from the stream — it must not short-circuit.");
    }

    // ─── Graceful false returns ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a mismatched hash returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenHashDoesNotMatch_ShouldReturnFalse()
    {
        using var algorithm = CreateAlgorithm();
        byte[] badHash = BitConverter.GetBytes((uint)1234);
        bool result = await algorithm.TryVerifyHashAsync(SampleData, badHash);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ShouldReturnFalse()
    {
        using var algorithm = CreateAlgorithm();
        bool result = await algorithm.TryVerifyHashAsync((Stream)null!, SampleHash);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null expected hash on the stream overload returns <see langword="false" />
    /// rather than throwing — the stream try-pattern treats this as a verification failure.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamExpectedHashIsNull_ShouldReturnFalse()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        bool result = await algorithm.TryVerifyHashAsync(stream, (byte[])null!);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that an <see cref="IOException" /> from a <see cref="FaultingStream" /> is
    /// swallowed and surfaces as <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceFaultsMidRead_ShouldReturnFalse()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new FaultingStream(
            CryptoTestUtilities.CreateDeterministicBytes(64), throwAfterBytes: 16);

        bool result = await algorithm.TryVerifyHashAsync(stream, new byte[4]);

        Assert.IsFalse(result,
            "TryVerifyHashAsync must return false when the source stream faults — IOException is swallowed by the try-pattern.");
    }

    /// <summary>
    /// Verifies that a <see cref="CancellationTriggerStream" /> cancelling the token mid-stream
    /// is swallowed and surfaces as <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenCancellationTriggeredMidStream_ShouldReturnFalse()
    {
        using var algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerStream(
            new IncrementingByteStream(1024), cts, cancelAfterRead: 2);

        bool result = await algorithm.TryVerifyHashAsync(stream, new byte[4], cts.Token);

        Assert.IsFalse(result,
            "TryVerifyHashAsync must return false when cancelled mid-stream — OperationCanceledException is swallowed by the try-pattern.");
    }

    // ─── Argument validation (non-stream overloads) ───────────────────────────────────────────
    // These overloads still throw ArgumentNullException: the byte-array overload rejects a null
    // input (unlike the stream overload, which returns false) and the string overload rejects
    // null string/encoding/hash arguments.

    /// <summary>
    /// Verifies that a null byte-array input raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayInputIsNull_ShouldThrowArgumentNullException()
    {
        using var algorithm = CreateAlgorithm();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.TryVerifyHashAsync((byte[])null!, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash on the byte-array overload raises
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayExpectedHashIsNull_ShouldThrowArgumentNullException()
    {
        using var algorithm = CreateAlgorithm();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.TryVerifyHashAsync(SampleData, (byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that a null string input raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringInputIsNull_ShouldThrowArgumentNullException()
    {
        using var algorithm = CreateAlgorithm();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.TryVerifyHashAsync(null!, SampleEncoding, SampleStringHash);
        });
    }

    /// <summary>
    /// Verifies that a null encoding raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        using var algorithm = CreateAlgorithm();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.TryVerifyHashAsync(SampleString, null!, SampleStringHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash on the string+encoding overload raises
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringExpectedHashIsNull_ShouldThrowArgumentNullException()
    {
        using var algorithm = CreateAlgorithm();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, null!);
        });
    }
}
