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
/// Every <see langword="null" /> data parameter (<c>input</c>, <c>expectedHash</c>,
/// <c>expectedHex</c>, <c>encoding</c>, <c>stream</c>) also surfaces as <see langword="false" />.
/// Only a <see langword="null" /> <c>algorithm</c> receiver still raises
/// <see cref="ArgumentNullException" />, since the extension method cannot dispatch without one.
/// </para>
/// <para>
/// The byte-array and string overloads run synchronously: they delegate to the corresponding
/// <c>TryVerifyHash</c> method and complete via <see cref="Task.FromResult{TResult}" />. A
/// pre-cancelled <see cref="CancellationToken" /> is honoured by short-circuiting to
/// <see langword="false" />.
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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(SampleData, SampleHash);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when the
    /// expected hash is supplied as a hex string.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayMatchesHex_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(SampleData, SampleHex);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching string + encoding input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenEncodedStringMatches_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, SampleStringHash);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected
    /// hash is supplied as a byte array.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        var result = await algorithm.TryVerifyHashAsync(stream, SampleHash);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected
    /// hash is supplied as a hex string.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        var result = await algorithm.TryVerifyHashAsync(stream, SampleHex);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected
    /// hash is supplied as a <see cref="ReadOnlyMemory{T}" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        ReadOnlyMemory<byte> expected = SampleHash;
        var result = await algorithm.TryVerifyHashAsync(stream, expected);
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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
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
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
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
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
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
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expectedHash = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var baseStream = new MemoryStream(new byte[] { 2, 3 });
        using var monitored = new MonitoringStream(baseStream);
        var expected = BitConverter.GetBytes((uint)5); // additive hash of { 2, 3 }

        var result = await algorithm.TryVerifyHashAsync(monitored, expected);

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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var badHash = BitConverter.GetBytes((uint)1234);
        var result = await algorithm.TryVerifyHashAsync(SampleData, badHash);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync((Stream)null!, SampleHash);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null expected hash on the stream overload returns <see langword="false" />
    /// rather than throwing — the stream try-pattern treats this as a verification failure.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamExpectedHashIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        var result = await algorithm.TryVerifyHashAsync(stream, (byte[])null!);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that an <see cref="IOException" /> from a <see cref="FaultingStream" /> is
    /// swallowed and surfaces as <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenSourceFaultsMidRead_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new FaultingStream(
            CryptoTestUtilities.CreateDeterministicBytes(64), throwAfterBytes: 16);

        var result = await algorithm.TryVerifyHashAsync(stream, new byte[4]);

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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerStream(
            new IncrementingByteStream(1024), cts, cancelAfterRead: 2);

        var result = await algorithm.TryVerifyHashAsync(stream, new byte[4], cts.Token);

        Assert.IsFalse(result,
            "TryVerifyHashAsync must return false when cancelled mid-stream — OperationCanceledException is swallowed by the try-pattern.");
    }

    /// <summary>
    /// Verifies that a null expected hex on the stream overload returns <see langword="false" />
    /// rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamExpectedHexIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        var result = await algorithm.TryVerifyHashAsync(stream, (string)null!);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a pre-cancelled token short-circuits the byte-array overload to
    /// <see langword="false" /> instead of executing the verification.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenTokenIsPreCancelled_ForByteArray_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await algorithm.TryVerifyHashAsync(SampleData, SampleHash, cts.Token);

        Assert.IsFalse(result);
    }

    // ─── Graceful false returns (non-stream overloads) ────────────────────────────────────────
    // The byte-array and string overloads now delegate synchronously to TryVerifyHash and follow
    // the same contract: every null data parameter returns false instead of throwing.

    /// <summary>
    /// Verifies that a null byte-array input returns <see langword="false" /> rather than
    /// throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayInputIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync((byte[])null!, SampleHash);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null expected hash on the byte-array overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayExpectedHashIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(SampleData, (byte[])null!);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null expected hex on the byte-array overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayExpectedHexIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(SampleData, (string)null!);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null string input on the string+encoding overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringInputIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(null!, SampleEncoding, SampleStringHash);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null encoding on the string+encoding overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenEncodingIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(SampleString, null!, SampleStringHash);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null expected hash on the string+encoding overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringExpectedHashIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, null!);
        Assert.IsFalse(result);
    }
}
