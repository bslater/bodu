// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Bodu.Test.IO;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for the synchronous <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm, byte[])" />
/// family — covering byte-array, offset/range, span, memory, and stream overloads.
/// </summary>
public partial class SymmetricAlgorithmExtensionTests
{
    // ─── Encrypt(byte[]) ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the byte-array overload throws <see cref="ArgumentNullException" /> when
    /// the algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArray_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        SymmetricAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Encrypt(new byte[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that the byte-array overload throws <see cref="ArgumentNullException" /> when
    /// the input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArray_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Encrypt(null!));
    }

    /// <summary>
    /// Verifies that the byte-array overload produces a non-empty result for valid input.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArray_WhenInputIsValid_ShouldReturnNonEmptyResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("encrypt-me");

        var cipherText = algorithm.Encrypt(plainText);

        Assert.IsNotNull(cipherText);
        Assert.IsTrue(cipherText.Length > 0);
    }

    /// <summary>
    /// Verifies that the byte-array overload produces output that round-trips back to the
    /// original plaintext via <c>Decrypt</c>.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArray_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("encrypt");

        var cipherText = algorithm.Encrypt(plainText);
        var decrypted = algorithm.Decrypt(cipherText);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    // ─── Encrypt(byte[], int) — offset-to-end overload ────────────────────────────────────────

    /// <summary>
    /// Verifies that the offset overload throws <see cref="ArgumentNullException" /> when the
    /// input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayOffset_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Encrypt(null!, 0));
    }

    /// <summary>
    /// Verifies that the offset overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the offset is negative.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayOffset_WhenOffsetIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Encrypt(data, -1));
    }

    /// <summary>
    /// Verifies that the offset overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the offset exceeds the array length.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayOffset_WhenOffsetExceedsBounds_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Encrypt(data, data.Length + 1));
    }

    /// <summary>
    /// Verifies that the offset overload with <c>offset = 0</c> encrypts the full array and
    /// round-trips back to the original plaintext.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayOffset_WhenOffsetIsZero_ShouldRoundTripCorrectly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("abc");

        var cipherText = algorithm.Encrypt(plainText, 0);
        var decrypted = algorithm.Decrypt(cipherText);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    // ─── Encrypt(byte[], int, int) — offset+count overload ────────────────────────────────────

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentNullException" /> when the
    /// input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayRange_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Encrypt(null!, 0, 4));
    }

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the offset is negative.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayRange_WhenOffsetIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Encrypt(data, -1, 2));
    }

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the count is negative.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayRange_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Encrypt(data, 0, -1));
    }

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>offset + count</c> exceeds the array length.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayRange_WhenOffsetPlusCountExceedsLength_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Encrypt(data, 2, 5));
    }

    /// <summary>
    /// Verifies that a valid (offset, count) range encrypts the specified slice and round-trips
    /// back to the original plaintext.
    /// </summary>
    [TestMethod]
    public void Encrypt_ByteArrayRange_WhenValid_ShouldRoundTripCorrectly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("hello");

        var cipherText = algorithm.Encrypt(plainText, 0, plainText.Length);
        var decrypted = algorithm.Decrypt(cipherText);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    // ─── Encrypt(ReadOnlySpan<byte>) ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the span overload throws <see cref="ArgumentNullException" /> when the
    /// algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_Span_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        SymmetricAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Encrypt((ReadOnlySpan<byte>)new byte[] { 1, 2, 3, 4 }));
    }

    /// <summary>
    /// Verifies that the span overload produces a non-empty result for valid input.
    /// </summary>
    [TestMethod]
    public void Encrypt_Span_WhenInputIsValid_ShouldReturnNonEmptyResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes("span-encrypt");

        var cipherText = algorithm.Encrypt(input);

        Assert.IsNotNull(cipherText);
        Assert.IsTrue(cipherText.Length > 0);
    }

    /// <summary>
    /// Verifies that the span overload produces output that round-trips back to the original
    /// plaintext.
    /// </summary>
    [TestMethod]
    public void Encrypt_Span_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("span-round-trip");

        var cipherText = algorithm.Encrypt((ReadOnlySpan<byte>)plainText);
        var decrypted = algorithm.Decrypt(cipherText);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    /// <summary>
    /// Verifies that the span overload produces output identical to the byte-array overload for
    /// the same input — confirming the two entry points share a code path.
    /// </summary>
    [TestMethod]
    public void Encrypt_Span_WhenComparedToByteArrayOverload_ShouldProduceIdenticalOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("identical-output");

        var fromArray = algorithm.Encrypt(plainText);
        var fromSpan = algorithm.Encrypt((ReadOnlySpan<byte>)plainText);

        CollectionAssert.AreEqual(fromArray, fromSpan);
    }

    // ─── Encrypt(ReadOnlyMemory<byte>) ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the memory overload throws <see cref="ArgumentNullException" /> when the
    /// algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_Memory_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        SymmetricAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Encrypt(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 })));
    }

    /// <summary>
    /// Verifies that the memory overload produces output that round-trips back to the original
    /// plaintext.
    /// </summary>
    [TestMethod]
    public void Encrypt_Memory_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("memory-encrypt");

        var cipherText = algorithm.Encrypt(new ReadOnlyMemory<byte>(plainText));
        var decrypted = algorithm.Decrypt(cipherText);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    /// <summary>
    /// Verifies that the memory overload produces output identical to the span overload for the
    /// same input.
    /// </summary>
    [TestMethod]
    public void Encrypt_Memory_WhenComparedToSpanOverload_ShouldProduceIdenticalOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("identical-memory");

        var fromSpan = algorithm.Encrypt((ReadOnlySpan<byte>)plainText);
        var fromMemory = algorithm.Encrypt(new ReadOnlyMemory<byte>(plainText));

        CollectionAssert.AreEqual(fromSpan, fromMemory);
    }

    // ─── Encrypt(Stream, Stream) — default buffer size overload ───────────────────────────────

    /// <summary>
    /// Verifies that the default-buffer stream overload throws <see cref="ArgumentNullException" />
    /// when the algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        SymmetricAlgorithm? algorithm = null;
        using var source = new MemoryStream();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Encrypt(source, target));
    }

    /// <summary>
    /// Verifies that the default-buffer stream overload throws <see cref="ArgumentNullException" />
    /// when the source stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Encrypt(null!, target));
    }

    /// <summary>
    /// Verifies that the default-buffer stream overload throws <see cref="ArgumentNullException" />
    /// when the target stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var source = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Encrypt(source, null!));
    }

    /// <summary>
    /// Verifies that the default-buffer stream overload produces output that round-trips back
    /// to the original plaintext.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("stream-encrypt-default-buffer");

        var cipherText = algorithm.Encrypt(plainText);

        using var cipherStream = new MemoryStream(cipherText);
        using var decryptedStream = new MemoryStream();
        algorithm.Decrypt(cipherStream, decryptedStream);

        CollectionAssert.AreEqual(plainText, decryptedStream.ToArray());
    }

    // ─── Encrypt(Stream, Stream, int) — explicit buffer size overload ─────────────────────────

    /// <summary>
    /// Verifies that the buffer-size stream overload throws <see cref="ArgumentOutOfRangeException" />
    /// when the buffer size is negative.
    /// </summary>
    [TestMethod]
    public void Encrypt_StreamWithBufferSize_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var source = new MemoryStream();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Encrypt(source, target, -128));
    }

    /// <summary>
    /// Verifies that the buffer-size stream overload throws <see cref="ArgumentOutOfRangeException" />
    /// when the buffer size is zero.
    /// </summary>
    [TestMethod]
    public void Encrypt_StreamWithBufferSize_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var source = new MemoryStream();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Encrypt(source, target, 0));
    }

    /// <summary>
    /// Verifies that the buffer-size stream overload throws <see cref="ArgumentNullException" />
    /// when the target stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_StreamWithBufferSize_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var source = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Encrypt(source, null!, 1024));
    }

    /// <summary>
    /// Verifies that the buffer-size stream overload produces output that round-trips back to
    /// the original plaintext.
    /// </summary>
    [TestMethod]
    public void Encrypt_StreamWithBufferSize_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("stream-encrypt-explicit-buffer");

        using var sourceStream = new MemoryStream(plainText);
        using var encryptedStream = new MemoryStream();
        algorithm.Encrypt(sourceStream, encryptedStream, bufferSize: 64);

        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();
        algorithm.Decrypt(encryptedStream, decryptedStream);

        CollectionAssert.AreEqual(plainText, decryptedStream.ToArray());
    }

    // ─── Stream-shape coverage ────────────────────────────────────────────────────────────────
    // The following tests exercise the sync encrypt read-accumulation loop against every
    // test-infrastructure stream shape that doesn't require a CancellationToken (the sync
    // overloads do not accept one). PaddingMode.None with block-aligned input isolates
    // stream-delivery behaviour from padding concerns.

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering plaintext 1 byte at a time
    /// produces correctly decryptable ciphertext, confirming the read loop handles the most
    /// extreme partial-read case.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenSourceIsFixedChunkStream_OneBytePerRead_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence64;

        using var input = new FixedChunkStream(plainText, chunkSize: 1);
        using var output = new MemoryStream();

        algorithm.Encrypt(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "Encrypt must produce correctly decryptable ciphertext from a 1-byte-per-read source.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> with a chunk size not aligned to the
    /// block size still produces correctly decryptable ciphertext.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenSourceIsFixedChunkStream_NonBlockAlignedChunk_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence128;

        // chunkSize=5 is not a multiple of the 16-byte block — exercises accumulation across chunk boundaries.
        using var input = new FixedChunkStream(plainText, chunkSize: 5);
        using var output = new MemoryStream();

        algorithm.Encrypt(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "Encrypt must produce correctly decryptable ciphertext from a non-block-aligned chunk stream.");
    }

    /// <summary>
    /// Verifies that an <see cref="IncrementingByteStream" /> — which returns at most half of
    /// its remaining bytes per read — produces correctly decryptable ciphertext, exercising the
    /// read loop under guaranteed partial reads.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenSourceIsIncrementingByteStream_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        // IncrementingByteStream(64) produces the same bytes as ByteSequence64.
        var plainText = CryptoTestUtilities.ByteSequence64;

        using var input = new IncrementingByteStream(64);
        using var output = new MemoryStream();

        algorithm.Encrypt(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "Encrypt must produce correctly decryptable ciphertext from an IncrementingByteStream.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedLengthIncrementingStream" /> — which delivers sequential
    /// bytes in partial reads — produces correctly decryptable ciphertext.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenSourceIsFixedLengthIncrementingStream_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        // FixedLengthIncrementingStream(64) produces the same bytes as ByteSequence64.
        var plainText = CryptoTestUtilities.ByteSequence64;

        using var input = new FixedLengthIncrementingStream(64);
        using var output = new MemoryStream();

        algorithm.Encrypt(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "Encrypt must produce correctly decryptable ciphertext from a FixedLengthIncrementingStream.");
    }

    /// <summary>
    /// Verifies that a <see cref="NonSeekableStream" /> is accepted, confirming the sync
    /// encrypt path never calls seek-related members on its source.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenSourceIsNonSeekable_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence128;

        using var input = new NonSeekableStream(plainText);
        using var output = new MemoryStream();

        algorithm.Encrypt(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, algorithm.Decrypt(output.ToArray()),
            "Encrypt from a NonSeekableStream must produce correctly decryptable ciphertext.");
    }

    // ─── Error propagation ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an <see cref="IOException" /> raised mid-read by a
    /// <see cref="FaultingStream" /> propagates out of the sync encrypt overload unmodified.
    /// </summary>
    [TestMethod]
    public void Encrypt_Stream_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        // Fault after 32 bytes — mid-way through a 128-byte input.
        using var input = new FaultingStream(CryptoTestUtilities.ByteSequence128, throwAfterBytes: 32);
        using var output = new MemoryStream();

        Assert.ThrowsExactly<IOException>(() =>
            algorithm.Encrypt(input, output, bufferSize: 16),
            "Encrypt must propagate IOException from a faulting source stream.");
    }
}
