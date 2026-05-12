// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests.EncryptAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for <see cref="SymmetricAlgorithmExtensions.EncryptAsync" /> covering argument
/// validation, correctness, stream-shape behaviours, cancellation, and error propagation.
/// </summary>
/// <remarks>
/// <c>EncryptAsync</c> propagates all exceptions — <see cref="IOException" />,
/// <see cref="OperationCanceledException" />, and null-argument errors — to the caller rather
/// than swallowing them.
/// </remarks>
public partial class SymmetricAlgorithmExtensionTests
{
    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a negative buffer size raises <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var input = new MemoryStream();
        using var output = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await algorithm.EncryptAsync(input, output, -1));
    }

    /// <summary>
    /// Verifies that a null target stream raises <see cref="ArgumentNullException" />.
    /// </summary>
    /// <remarks>
    /// <see cref="Stream.Null" /> is a valid non-null stream, so <c>null!</c> is the correct
    /// argument to trigger this guard.
    /// </remarks>
    [TestMethod]
    public async Task EncryptAsync_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var input = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            algorithm.EncryptAsync(input, null!));
    }

    // ─── Correctness — round-trip ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty input stream combined with <see cref="PaddingMode.None" />
    /// produces an empty output stream.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenInputIsEmptyStream_ShouldProduceEmptyOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None; // no padding → empty in, empty out

        using var input = new MemoryStream(Array.Empty<byte>());
        using var output = new MemoryStream();

        await algorithm.EncryptAsync(input, output);

        Assert.AreEqual(0, output.Length,
            "Empty input with PaddingMode.None should produce empty output.");
    }

    /// <summary>
    /// Verifies that encrypting from a partial-read input stream produces correctly decryptable
    /// ciphertext, confirming the async read-accumulation loop handles partial reads correctly.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenSourceIsIncrementingByteStream_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        // IncrementingByteStream(64) produces the same byte sequence as ByteSequence64
        // but delivers it in partial reads, exercising the read loop.
        var plainText = CryptoTestUtilities.ByteSequence64;

        using var input = new IncrementingByteStream(64);
        using var output = new MemoryStream();

        await algorithm.EncryptAsync(input, output, bufferSize: 16);

        var decrypted = algorithm.Decrypt(output.ToArray());
        CollectionAssert.AreEqual(plainText, decrypted,
            "EncryptAsync with a partial-read input stream must produce correctly decryptable ciphertext.");
    }

    /// <summary>
    /// Verifies that encrypting to a throttled output stream still produces correctly
    /// decryptable ciphertext.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenOutputIsThrottled_ShouldStillProduceResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var inputBytes = CryptoTestUtilities.ByteSequence128;

        using var input = new MemoryStream(inputBytes);
        using var throttledOutput = new ThrottledOutputMemoryStream(delayMilliseconds: 50);

        await algorithm.EncryptAsync(input, throttledOutput, bufferSize: 32);

        var decrypted = algorithm.Decrypt(throttledOutput.ToArray());
        CollectionAssert.AreEqual(inputBytes, decrypted,
            "EncryptAsync to a throttled output stream must produce correctly decryptable ciphertext.");
    }

    /// <summary>
    /// Verifies a full round trip through throttled input and output streams — the slow-I/O
    /// equivalent of the happy path.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenUsingThrottledStreams_ShouldSucceed()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        // Capture the expected plaintext via ToArray() before reading the stream,
        // since reading triggers the per-byte delay.
        using var input = new ThrottledIncrementingByteStream(256, readDelay: 10);
        var expected = input.ToArray();

        using var encrypted = new ThrottledOutputMemoryStream(delayMilliseconds: 10);
        await algorithm.EncryptAsync(input, encrypted, bufferSize: 64, CancellationToken.None);

        using var cipherStream = new MemoryStream(encrypted.ToArray());
        using var decrypted = new MemoryStream();
        await algorithm.DecryptAsync(cipherStream, decrypted, bufferSize: 64, CancellationToken.None);

        CollectionAssert.AreEqual(expected, decrypted.ToArray(),
            "Round-trip encrypt+decrypt through throttled streams must recover the original plaintext.");
    }

    // ─── Stream-shape coverage ────────────────────────────────────────────────────────────────
    // The following tests exercise the async read-accumulation loop against every
    // test-infrastructure stream shape. PaddingMode.None with block-aligned input isolates
    // stream-delivery behaviour from padding concerns.

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering plaintext 1 byte at a time
    /// still produces correctly decryptable ciphertext, confirming the async read loop handles
    /// the most extreme partial-read case.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenSourceIsFixedChunkStream_OneBytePerRead_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence64;

        using var input = new FixedChunkStream(plainText, chunkSize: 1);
        using var output = new MemoryStream();

        await algorithm.EncryptAsync(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "EncryptAsync must produce correctly decryptable ciphertext from a 1-byte-per-read source.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> with a chunk size not aligned to the
    /// block size still produces correctly decryptable ciphertext.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenSourceIsFixedChunkStream_NonBlockAlignedChunk_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence128;

        // chunkSize=5 is not a multiple of the 16-byte block — exercises accumulation across chunk boundaries.
        using var input = new FixedChunkStream(plainText, chunkSize: 5);
        using var output = new MemoryStream();

        await algorithm.EncryptAsync(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "EncryptAsync must produce correctly decryptable ciphertext from a non-block-aligned chunk stream.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedLengthIncrementingStream" /> delivering sequential bytes
    /// in partial reads produces correctly decryptable ciphertext.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenSourceIsFixedLengthIncrementingStream_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        // FixedLengthIncrementingStream(64) produces the same bytes as ByteSequence64.
        var plainText = CryptoTestUtilities.ByteSequence64;

        using var input = new FixedLengthIncrementingStream(64);
        using var output = new MemoryStream();

        await algorithm.EncryptAsync(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "EncryptAsync from a FixedLengthIncrementingStream must produce correctly decryptable ciphertext.");
    }

    /// <summary>
    /// Verifies that a <see cref="NonSeekableStream" /> is accepted, confirming the async
    /// encrypt path never calls seek-related members on its source.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenSourceIsNonSeekable_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence128;

        using var input = new NonSeekableStream(plainText);
        using var output = new MemoryStream();

        await algorithm.EncryptAsync(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "EncryptAsync from a NonSeekableStream must produce correctly decryptable ciphertext.");
    }

    // ─── Error propagation ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an <see cref="IOException" /> raised mid-read by a
    /// <see cref="FaultingStream" /> propagates out of the method unmodified.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        // Fault after 32 bytes — mid-way through the third block of a 128-byte input.
        using var input = new FaultingStream(CryptoTestUtilities.ByteSequence128, throwAfterBytes: 32);
        using var output = new MemoryStream();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await algorithm.EncryptAsync(input, output, bufferSize: 16),
            "EncryptAsync must propagate IOException from a faulting source stream.");
    }

    /// <summary>
    /// Verifies that a cancellation token signalled by its timeout raises
    /// <see cref="TaskCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        // ThrottledIncrementingByteStream delays each read, ensuring the cancellation
        // deadline fires before all data has been consumed.
        using var input = new ThrottledIncrementingByteStream(512, readDelay: 1000);
        using var output = new ThrottledOutputMemoryStream(delayMilliseconds: 1000);
        using var cts = new CancellationTokenSource(millisecondsDelay: 100);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
            await algorithm.EncryptAsync(input, output, bufferSize: 32, cts.Token));
    }

    /// <summary>
    /// Verifies that a <see cref="CancellationTriggerStream" /> cancelling mid-stream causes the
    /// method to throw <see cref="OperationCanceledException" /> (or its subtype
    /// <see cref="TaskCanceledException" />).
    /// </summary>
    [TestMethod]
    public async Task EncryptAsync_WhenCancellationTriggeredMidStream_ShouldThrowOperationCanceledException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        using var cts = new CancellationTokenSource();
        using var inner = new MemoryStream(CryptoTestUtilities.ByteSequence128);
        using var input = new CancellationTriggerStream(inner, cts, cancelAfterRead: 2);
        using var output = new MemoryStream();

        // Use the try/catch idiom here rather than ThrowsAsync because async I/O can surface
        // cancellation as either OperationCanceledException or its TaskCanceledException subtype.
        try
        {
            await algorithm.EncryptAsync(input, output, bufferSize: 16, cts.Token);
            Assert.Fail("Expected OperationCanceledException.");
        }
        catch (OperationCanceledException)
        {
            // Expected — accept either the base type or TaskCanceledException.
        }
    }
}
