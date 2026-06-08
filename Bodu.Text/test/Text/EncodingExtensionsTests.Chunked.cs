// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Chunked.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeChunk(System.Text.Encoder, ReadOnlySpan{char}, Span{byte}, bool, out int, out int)" />
    /// reports <see cref="OperationStatus.Done" /> and writes all bytes when the destination fits.
    /// </summary>
    [TestMethod]
    public void EncodeChunk_WhenDestinationFits_ShouldReturnDoneAndWriteAllBytes()
    {
        System.Text.Encoder encoder = System.Text.Encoding.UTF8.GetEncoder();
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required + 16];

        OperationStatus status = encoder.EncodeChunk(
            MultiByteText,
            destination,
            flush: true,
            out var charsConsumed,
            out var bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(MultiByteText.Length, charsConsumed);
        Assert.AreEqual(required, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeChunk(System.Text.Encoder, ReadOnlySpan{char}, Span{byte}, bool, out int, out int)" />
    /// reports <see cref="OperationStatus.DestinationTooSmall" /> when the destination cannot fit the full
    /// chunk, leaving the encoder in a state that can resume on a subsequent call.
    /// </summary>
    [TestMethod]
    public void EncodeChunk_WhenDestinationIsTooSmall_ShouldReturnDestinationTooSmallAndAllowResume()
    {
        System.Text.Encoder encoder = System.Text.Encoding.UTF8.GetEncoder();
        var total = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        var firstChunk = new byte[total / 2];

        OperationStatus first = encoder.EncodeChunk(
            MultiByteText,
            firstChunk,
            flush: false,
            out var charsConsumedFirst,
            out var bytesWrittenFirst);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, first);
        Assert.IsTrue(bytesWrittenFirst > 0);

        var secondChunk = new byte[total];
        OperationStatus second = encoder.EncodeChunk(
            MultiByteText.AsSpan(charsConsumedFirst),
            secondChunk,
            flush: true,
            out var charsConsumedSecond,
            out var bytesWrittenSecond);

        Assert.AreEqual(OperationStatus.Done, second);
        Assert.AreEqual(MultiByteText.Length, charsConsumedFirst + charsConsumedSecond);
        Assert.AreEqual(total, bytesWrittenFirst + bytesWrittenSecond);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeChunk(System.Text.Encoder, ReadOnlySpan{char}, Span{byte}, bool, out int, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoder</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncodeChunk_WhenEncoderIsNull_ShouldThrowExactly()
    {
        var backing = new byte[16];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.EncodeChunk(null!, SampleText, backing, false, out _, out _);
        });

        Assert.AreEqual("encoder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeChunk(System.Text.Decoder, ReadOnlySpan{byte}, Span{char}, bool, out int, out int)" />
    /// reports <see cref="OperationStatus.Done" /> and writes all characters when the destination fits.
    /// </summary>
    [TestMethod]
    public void DecodeChunk_WhenDestinationFits_ShouldReturnDoneAndWriteAllChars()
    {
        System.Text.Decoder decoder = System.Text.Encoding.UTF8.GetDecoder();
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        Span<char> destination = new char[MultiByteText.Length + 8];

        OperationStatus status = decoder.DecodeChunk(
            bytes,
            destination,
            flush: true,
            out var bytesConsumed,
            out var charsWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(bytes.Length, bytesConsumed);
        Assert.AreEqual(MultiByteText.Length, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeChunk(System.Text.Decoder, ReadOnlySpan{byte}, Span{char}, bool, out int, out int)" />
    /// resumes correctly across two calls when the destination of the first call is undersized.
    /// </summary>
    [TestMethod]
    public void DecodeChunk_WhenDestinationIsTooSmall_ShouldReturnDestinationTooSmallAndAllowResume()
    {
        System.Text.Decoder decoder = System.Text.Encoding.UTF8.GetDecoder();
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var firstChunk = new char[MultiByteText.Length / 2];

        OperationStatus first = decoder.DecodeChunk(
            bytes,
            firstChunk,
            flush: false,
            out var bytesConsumedFirst,
            out var charsWrittenFirst);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, first);
        Assert.IsTrue(charsWrittenFirst > 0);

        var secondChunk = new char[MultiByteText.Length];
        OperationStatus second = decoder.DecodeChunk(
            bytes.AsSpan(bytesConsumedFirst),
            secondChunk,
            flush: true,
            out var bytesConsumedSecond,
            out var charsWrittenSecond);

        Assert.AreEqual(OperationStatus.Done, second);
        Assert.AreEqual(bytes.Length, bytesConsumedFirst + bytesConsumedSecond);
        Assert.AreEqual(MultiByteText.Length, charsWrittenFirst + charsWrittenSecond);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeChunk(System.Text.Decoder, ReadOnlySpan{byte}, Span{char}, bool, out int, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>decoder</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DecodeChunk_WhenDecoderIsNull_ShouldThrowExactly()
    {
        var backing = new char[16];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.DecodeChunk(null!, [0x68], backing, false, out _, out _);
        });

        Assert.AreEqual("decoder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeChunk(System.Text.Decoder, ReadOnlySpan{byte}, Span{char}, bool, out int, out int)" />
    /// preserves partial multi-byte sequences across chunk boundaries when called with <c>flush: false</c>.
    /// </summary>
    [TestMethod]
    public void DecodeChunk_WhenSourceEndsInPartialMultiByteSequence_ShouldRetainStateUntilFlush()
    {
        System.Text.Decoder decoder = System.Text.Encoding.UTF8.GetDecoder();
        var fullBytes = System.Text.Encoding.UTF8.GetBytes("é");
        Assert.AreEqual(2, fullBytes.Length);
        var firstHalf = new byte[] { fullBytes[0] };
        var secondHalf = new byte[] { fullBytes[1] };
        var destination = new char[4];

        OperationStatus first = decoder.DecodeChunk(
            firstHalf,
            destination,
            flush: false,
            out var bytesConsumedFirst,
            out var charsWrittenFirst);

        Assert.AreEqual(OperationStatus.Done, first);
        Assert.AreEqual(1, bytesConsumedFirst);
        Assert.AreEqual(0, charsWrittenFirst);

        OperationStatus second = decoder.DecodeChunk(
            secondHalf,
            destination,
            flush: true,
            out var bytesConsumedSecond,
            out var charsWrittenSecond);

        Assert.AreEqual(OperationStatus.Done, second);
        Assert.AreEqual(1, bytesConsumedSecond);
        Assert.AreEqual(1, charsWrittenSecond);
        Assert.AreEqual('é', destination[0]);
    }
}
