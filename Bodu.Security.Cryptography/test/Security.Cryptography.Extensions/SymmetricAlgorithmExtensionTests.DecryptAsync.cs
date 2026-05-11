// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests.DecryptAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for <see cref="SymmetricAlgorithmExtensions.DecryptAsync" /> covering argument
/// validation, padding-mode behaviour for empty input, correctness against throttled streams,
/// stream-shape coverage, cancellation, and error propagation.
/// </summary>
/// <remarks>
/// <c>DecryptAsync</c> propagates all exceptions — <see cref="IOException" />,
/// <see cref="OperationCanceledException" />, and null-argument errors — to the caller rather
/// than swallowing them.
/// </remarks>
public partial class SymmetricAlgorithmExtensionTests
{
    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a zero buffer size raises <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var input = new MemoryStream();
        using var output = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await algorithm.DecryptAsync(input, output, 0));
    }

    /// <summary>
    /// Verifies that a null source stream raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var output = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.DecryptAsync(null!, output));
    }

    /// <summary>
    /// Verifies that a null output stream raises <see cref="ArgumentNullException" />.
    /// </summary>
    /// <remarks>
    /// The null-output check must fire before any decryption is attempted, so the input
    /// content is irrelevant — a single zeroed block is sufficient.
    /// </remarks>
    [TestMethod]
    public async Task DecryptAsync_WhenOutputIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var input = new MemoryStream(new byte[algorithm.BlockSize / 8]);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            algorithm.DecryptAsync(input, null!, CancellationToken.None));
    }

    // ─── Padding-mode behaviour on empty input ────────────────────────────────────────────────

    /// <summary>
    /// Verifies the padding-mode contract for empty input: modes that don't require a final
    /// padding block (<see cref="PaddingMode.None" />, <see cref="PaddingMode.Zeros" />) should
    /// succeed and produce empty output; modes that do require one
    /// (<see cref="PaddingMode.PKCS7" />, <see cref="PaddingMode.ANSIX923" />,
    /// <see cref="PaddingMode.ISO10126" />) should raise <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    [DataRow(PaddingMode.None, true)]
    [DataRow(PaddingMode.PKCS7, false)]
    [DataRow(PaddingMode.ANSIX923, false)]
    [DataRow(PaddingMode.ISO10126, false)]
    [DataRow(PaddingMode.Zeros, true)]
    public async Task DecryptAsync_EmptyInput_WithVariousPaddingModes_ShouldBehaveCorrectly(
        PaddingMode padding, bool expectSuccess)
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = padding;

        using var input = new MemoryStream(Array.Empty<byte>());
        using var output = new MemoryStream();

        if (expectSuccess)
        {
            await algorithm.DecryptAsync(input, output);
            Assert.AreEqual(0, output.Length,
                $"Empty input with {padding} padding should produce empty output.");
        }
        else
        {
            await Assert.ThrowsExactlyAsync<CryptographicException>(() =>
                algorithm.DecryptAsync(input, output),
                $"Empty input with {padding} padding should throw CryptographicException.");
        }
    }

    // ─── Correctness — round-trip ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the target stream remains open and writable after a successful
    /// <c>DecryptAsync</c> call, confirming the method does not dispose the caller-owned stream.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenCalled_WithNoPadding_ShouldNotDisposeTargetStream()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        using var input = new MemoryStream();
        using var output = new MemoryStream();

        await algorithm.DecryptAsync(input, output);

        Assert.IsTrue(output.CanWrite,
            "Output stream should remain open and writable after DecryptAsync.");

        // Confirm the stream is genuinely still usable, not just reporting CanWrite=true.
        output.WriteByte(0xFF);
    }

    /// <summary>
    /// Verifies that decrypting with a small buffer size still recovers the original plaintext.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenUsingSmallBufferSize_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        byte[] plainText = CryptoTestUtilities.ByteSequence64;
        byte[] encrypted = algorithm.Encrypt(plainText);

        using var input = new MemoryStream(encrypted);
        using var output = new MemoryStream();

        await algorithm.DecryptAsync(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, output.ToArray(),
            "DecryptAsync must recover the original plaintext when reading in small buffer chunks.");
    }

    /// <summary>
    /// Verifies that decrypting to a throttled output stream still recovers the original
    /// plaintext.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenOutputIsThrottled_ShouldStillProduceResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        byte[] original = CryptoTestUtilities.ByteSequence128;
        byte[] encrypted = algorithm.Encrypt(original);

        using var input = new MemoryStream(encrypted);
        using var throttledOutput = new ThrottledOutputMemoryStream(delayMilliseconds: 50);

        await algorithm.DecryptAsync(input, throttledOutput, bufferSize: 32);

        CollectionAssert.AreEqual(original, throttledOutput.ToArray(),
            "DecryptAsync must recover the original plaintext even when writing to a throttled stream.");
    }

    // ─── Stream-shape coverage ────────────────────────────────────────────────────────────────
    // The following tests exercise the async read-accumulation loop against every
    // test-infrastructure stream shape. PaddingMode.None with block-aligned input isolates
    // stream-delivery behaviour from padding concerns.

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering ciphertext 1 byte at a time
    /// still recovers the original plaintext after decryption.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenSourceIsFixedChunkStream_OneBytePerRead_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        byte[] plainText = CryptoTestUtilities.ByteSequence64;
        byte[] cipherText = algorithm.Encrypt(plainText);

        using var input = new FixedChunkStream(cipherText, chunkSize: 1);
        using var output = new MemoryStream();

        await algorithm.DecryptAsync(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, output.ToArray(),
            "DecryptAsync must recover original plaintext from a 1-byte-per-read ciphertext source.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering ciphertext in non-block-aligned
    /// chunks still recovers the original plaintext after decryption.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenSourceIsFixedChunkStream_NonBlockAlignedChunk_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        byte[] plainText = CryptoTestUtilities.ByteSequence128;
        byte[] cipherText = algorithm.Encrypt(plainText);

        using var input = new FixedChunkStream(cipherText, chunkSize: 5);
        using var output = new MemoryStream();

        await algorithm.DecryptAsync(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, output.ToArray(),
            "DecryptAsync must recover original plaintext from a non-block-aligned chunk ciphertext source.");
    }

    /// <summary>
    /// Verifies that a <see cref="NonSeekableStream" /> is accepted, confirming the async
    /// decrypt path never calls seek-related members on its source.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenSourceIsNonSeekable_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        byte[] plainText = CryptoTestUtilities.ByteSequence128;
        byte[] cipherText = algorithm.Encrypt(plainText);

        using var input = new NonSeekableStream(cipherText);
        using var output = new MemoryStream();

        await algorithm.DecryptAsync(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, output.ToArray(),
            "DecryptAsync from a NonSeekableStream must recover the original plaintext.");
    }

    // ─── Error propagation ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an <see cref="IOException" /> raised mid-read by a
    /// <see cref="FaultingStream" /> propagates out of the method unmodified.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        byte[] cipherText = algorithm.Encrypt(CryptoTestUtilities.ByteSequence128);

        // Fault after 32 bytes — mid-way through the ciphertext stream.
        using var input = new FaultingStream(cipherText, throwAfterBytes: 32);
        using var output = new MemoryStream();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await algorithm.DecryptAsync(input, output, bufferSize: 16),
            "DecryptAsync must propagate IOException from a faulting ciphertext stream.");
    }

    /// <summary>
    /// Verifies that a cancellation token signalled by its timeout raises
    /// <see cref="TaskCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        // Use a block-aligned deterministic input large enough that the throttled
        // output write does not complete before the cancellation deadline fires.
        byte[] plainText = CryptoTestUtilities.ByteSequence256;
        byte[] encrypted = algorithm.Encrypt(plainText);

        using var input = new MemoryStream(encrypted);
        using var output = new ThrottledOutputMemoryStream(delayMilliseconds: 1000);
        using var cts = new CancellationTokenSource(millisecondsDelay: 100);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() =>
            algorithm.DecryptAsync(input, output, bufferSize: 64, cts.Token));
    }

    /// <summary>
    /// Verifies that a <see cref="CancellationTriggerStream" /> cancelling mid-stream causes the
    /// method to throw <see cref="OperationCanceledException" /> (or its subtype
    /// <see cref="TaskCanceledException" />).
    /// </summary>
    [TestMethod]
    public async Task DecryptAsync_WhenCancellationTriggeredMidStream_ShouldThrowOperationCanceledException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        byte[] cipherText = algorithm.Encrypt(CryptoTestUtilities.ByteSequence128);

        using var cts = new CancellationTokenSource();
        using var inner = new MemoryStream(cipherText);
        using var input = new CancellationTriggerStream(inner, cts, cancelAfterRead: 2);
        using var output = new MemoryStream();

        // Use the try/catch idiom here rather than ThrowsAsync because async I/O can surface
        // cancellation as either OperationCanceledException or its TaskCanceledException subtype.
        try
        {
            await algorithm.DecryptAsync(input, output, bufferSize: 16, cts.Token);
            Assert.Fail("Expected OperationCanceledException.");
        }
        catch (OperationCanceledException)
        {
            // Expected — accept either the base type or TaskCanceledException.
        }
    }
}
