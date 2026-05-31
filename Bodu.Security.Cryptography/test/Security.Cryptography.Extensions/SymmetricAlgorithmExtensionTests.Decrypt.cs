// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests.Decrypt.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Bodu.Test.IO;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for the synchronous <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm, byte[])" />
/// family — covering byte-array, offset/range, span, memory, and stream overloads.
/// </summary>
public partial class SymmetricAlgorithmExtensionTests
{
    // ─── Decrypt(byte[]) ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the byte-array overload throws <see cref="ArgumentNullException" /> when
    /// the algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArray_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        SymmetricAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Decrypt([1, 2, 3, 4]));
    }

    /// <summary>
    /// Verifies that the byte-array overload throws <see cref="ArgumentNullException" /> when
    /// the input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArray_WhenArrayIsNull_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Decrypt(null!));
    }

    /// <summary>
    /// Verifies that the byte-array overload correctly decrypts ciphertext produced by the
    /// matching <c>Encrypt</c> overload.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArray_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("abc");

        var cipherText = algorithm.Encrypt(plainText);
        var decrypted = algorithm.Decrypt(cipherText);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    // ─── Decrypt(byte[], int) — offset-to-end overload ────────────────────────────────────────

    /// <summary>
    /// Verifies that the offset overload throws <see cref="ArgumentNullException" /> when the
    /// input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayOffset_WhenArrayIsNull_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Decrypt(null!, 0));
    }

    /// <summary>
    /// Verifies that the offset overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the offset is negative.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayOffset_WhenOffsetIsNegative_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Decrypt(data, -1));
    }

    /// <summary>
    /// Verifies that the offset overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the offset exceeds the array bounds.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayOffset_WhenOffsetExceedsBounds_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = new byte[10];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Decrypt(data, 20));
    }

    /// <summary>
    /// Verifies that the offset overload with <c>offset = 0</c> correctly decrypts the full
    /// array.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayOffset_WhenOffsetIsZero_ShouldRoundTripCorrectly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("abc");

        var cipherText = algorithm.Encrypt(plainText);
        var decrypted = algorithm.Decrypt(cipherText, 0);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    // ─── Decrypt(byte[], int, int) — offset+count overload ────────────────────────────────────

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentNullException" /> when the
    /// input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayRange_WhenArrayIsNull_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Decrypt(null!, 0, 4));
    }

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the offset is negative.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayRange_WhenOffsetIsNegative_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Decrypt(data, -1, 2));
    }

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// the count is negative.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayRange_WhenCountIsNegative_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Decrypt(data, 0, -1));
    }

    /// <summary>
    /// Verifies that the range overload throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>offset + count</c> exceeds the array length.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayRange_WhenOffsetPlusCountExceedsLength_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var data = Encoding.UTF8.GetBytes("data");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Decrypt(data, 2, 5));
    }

    /// <summary>
    /// Verifies that a valid (offset, count) range correctly decrypts the specified slice.
    /// </summary>
    [TestMethod]
    public void Decrypt_ByteArrayRange_WhenValid_ShouldRoundTripCorrectly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("abcde");

        var cipherText = algorithm.Encrypt(plainText);
        var decrypted = algorithm.Decrypt(cipherText, 0, cipherText.Length);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    // ─── Decrypt(ReadOnlySpan<byte>) ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the span overload throws <see cref="ArgumentNullException" /> when the
    /// algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_Span_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        SymmetricAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Decrypt((ReadOnlySpan<byte>)[1, 2, 3, 4]));
    }

    /// <summary>
    /// Verifies that the span overload correctly decrypts span-wrapped ciphertext back to the
    /// original plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_Span_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("span-decrypt");

        var cipherText = algorithm.Encrypt(plainText);
        var decrypted = algorithm.Decrypt((ReadOnlySpan<byte>)cipherText);

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    /// <summary>
    /// Verifies that the span overload produces output identical to the byte-array overload
    /// for the same ciphertext input.
    /// </summary>
    [TestMethod]
    public void Decrypt_Span_WhenComparedToByteArrayOverload_ShouldProduceIdenticalOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("identical-decrypt");
        var cipherText = algorithm.Encrypt(plainText);

        var fromArray = algorithm.Decrypt(cipherText);
        var fromSpan = algorithm.Decrypt((ReadOnlySpan<byte>)cipherText);

        CollectionAssert.AreEqual(fromArray, fromSpan);
    }

    // ─── Decrypt(ReadOnlyMemory<byte>) ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the memory overload throws <see cref="ArgumentNullException" /> when the
    /// algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_Memory_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        SymmetricAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Decrypt(new ReadOnlyMemory<byte>([1, 2, 3, 4])));
    }

    /// <summary>
    /// Verifies that the memory overload correctly decrypts memory-wrapped ciphertext back to
    /// the original plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_Memory_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("memory-decrypt");

        var cipherText = algorithm.Encrypt(plainText);
        var decrypted = algorithm.Decrypt(new ReadOnlyMemory<byte>(cipherText));

        CollectionAssert.AreEqual(plainText, decrypted);
    }

    /// <summary>
    /// Verifies that the memory overload produces output identical to the span overload for
    /// the same ciphertext input.
    /// </summary>
    [TestMethod]
    public void Decrypt_Memory_WhenComparedToSpanOverload_ShouldProduceIdenticalOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("memory-vs-span");
        var cipherText = algorithm.Encrypt(plainText);

        var fromSpan = algorithm.Decrypt((ReadOnlySpan<byte>)cipherText);
        var fromMemory = algorithm.Decrypt(new ReadOnlyMemory<byte>(cipherText));

        CollectionAssert.AreEqual(fromSpan, fromMemory);
    }

    // ─── Decrypt(Stream, Stream) — default buffer size overload ───────────────────────────────

    /// <summary>
    /// Verifies that the default-buffer stream overload throws <see cref="ArgumentNullException" />
    /// when the algorithm receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        SymmetricAlgorithm? algorithm = null;
        using var source = new MemoryStream();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.Decrypt(source, target));
    }

    /// <summary>
    /// Verifies that the default-buffer stream overload throws <see cref="ArgumentNullException" />
    /// when the source stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenSourceStreamIsNull_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Decrypt(null!, target));
    }

    /// <summary>
    /// Verifies that the default-buffer stream overload throws <see cref="ArgumentNullException" />
    /// when the target stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenTargetStreamIsNull_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var source = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Decrypt(source, null!));
    }

    /// <summary>
    /// Verifies that the default-buffer stream overload correctly decrypts stream content and
    /// round-trips to the original plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("stream-data");

        var cipherText = algorithm.Encrypt(plainText);
        using var source = new MemoryStream(cipherText);
        using var target = new MemoryStream();

        algorithm.Decrypt(source, target);

        CollectionAssert.AreEqual(plainText, target.ToArray());
    }

    // ─── Decrypt(Stream, Stream, int) — explicit buffer size overload ─────────────────────────

    /// <summary>
    /// Verifies that the buffer-size stream overload throws <see cref="ArgumentNullException" />
    /// when the source stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_StreamWithBufferSize_WhenSourceStreamIsNull_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm.Decrypt(null!, target, 1024));
    }

    /// <summary>
    /// Verifies that the buffer-size stream overload throws <see cref="ArgumentOutOfRangeException" />
    /// when the buffer size is zero.
    /// </summary>
    [TestMethod]
    public void Decrypt_StreamWithBufferSize_WhenBufferSizeIsZero_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        using var source = new MemoryStream();
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            algorithm.Decrypt(source, target, 0));
    }

    /// <summary>
    /// Verifies that the buffer-size stream overload correctly decrypts stream data and
    /// round-trips to the original plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_StreamWithBufferSize_WhenRoundTripped_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = Encoding.UTF8.GetBytes("explicit-buffer-decrypt");

        var cipherText = algorithm.Encrypt(plainText);
        using var source = new MemoryStream(cipherText);
        using var target = new MemoryStream();

        algorithm.Decrypt(source, target, bufferSize: 64);

        CollectionAssert.AreEqual(plainText, target.ToArray());
    }

    // ─── Stream-shape coverage ────────────────────────────────────────────────────────────────
    // The following tests exercise the sync decrypt read-accumulation loop against every
    // test-infrastructure stream shape that doesn't require a CancellationToken (the sync
    // overloads do not accept one). PaddingMode.None with block-aligned input isolates
    // stream-delivery behaviour from padding concerns.

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering ciphertext 1 byte at a time
    /// still recovers the original plaintext after decryption.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenSourceIsFixedChunkStream_OneBytePerRead_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence64;
        var cipherText = algorithm.Encrypt(plainText);

        using var input = new FixedChunkStream(cipherText, chunkSize: 1);
        using var output = new MemoryStream();

        algorithm.Decrypt(input, output, bufferSize: 16);

        CollectionAssert.AreEqual(plainText, output.ToArray(),
            "Decrypt must recover original plaintext from a 1-byte-per-read ciphertext source.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering ciphertext in non-block-aligned
    /// chunks still recovers the original plaintext after decryption.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenSourceIsFixedChunkStream_NonBlockAlignedChunk_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence128;
        var cipherText = algorithm.Encrypt(plainText);

        using var input = new FixedChunkStream(cipherText, chunkSize: 5);
        using var output = new MemoryStream();

        algorithm.Decrypt(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, output.ToArray(),
            "Decrypt must recover original plaintext from a non-block-aligned chunk ciphertext source.");
    }

    /// <summary>
    /// Verifies that a <see cref="NonSeekableStream" /> is accepted, confirming the sync
    /// decrypt path never calls seek-related members on its source.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenSourceIsNonSeekable_ShouldProduceCorrectResult()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var plainText = CryptoTestUtilities.ByteSequence128;
        var cipherText = algorithm.Encrypt(plainText);

        using var input = new NonSeekableStream(cipherText);
        using var output = new MemoryStream();

        algorithm.Decrypt(input, output, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, output.ToArray(),
            "Decrypt from a NonSeekableStream must recover the original plaintext.");
    }

    // ─── Error propagation ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an <see cref="IOException" /> raised mid-read by a
    /// <see cref="FaultingStream" /> propagates out of the sync decrypt overload unmodified.
    /// </summary>
    [TestMethod]
    public void Decrypt_Stream_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        var cipherText = algorithm.Encrypt(CryptoTestUtilities.ByteSequence128);

        // Fault after 32 bytes — mid-way through the ciphertext stream.
        using var input = new FaultingStream(cipherText, throwAfterBytes: 32);
        using var output = new MemoryStream();

        Assert.ThrowsExactly<IOException>(() =>
            algorithm.Decrypt(input, output, bufferSize: 16),
            "Decrypt must propagate IOException from a faulting ciphertext stream.");
    }
}
