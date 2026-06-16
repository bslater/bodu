// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Span.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetUtf8ByteCount(ReadOnlySpan{char})" /> matches the BCL
    /// UTF-8 byte count for canonical pattern inputs.
    /// </summary>
    /// <param name="key">The pattern key from <see cref="GetPatternText" />.</param>
    [TestMethod]
    [DataRow("empty")]
    [DataRow("ascii")]
    [DataRow("multi-byte")]
    [DataRow("single-emoji")]
    public void GetUtf8ByteCount_WhenInvoked_ShouldMatchBclUtf8(string key)
    {
        string text = GetPatternText(key);
        int expected = System.Text.Encoding.UTF8.GetByteCount(text);

        int actual = text.AsSpan().GetUtf8ByteCount();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToUtf8Bytes(ReadOnlySpan{char})" /> matches the BCL UTF-8
    /// byte sequence for canonical pattern inputs.
    /// </summary>
    /// <param name="key">The pattern key from <see cref="GetPatternText" />.</param>
    [TestMethod]
    [DataRow("empty")]
    [DataRow("ascii")]
    [DataRow("multi-byte")]
    [DataRow("single-emoji")]
    public void ToUtf8Bytes_WhenInvoked_ShouldMatchBclUtf8(string key)
    {
        string text = GetPatternText(key);
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(text);

        byte[] actual = text.AsSpan().ToUtf8Bytes();

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToUtf8Bytes(ReadOnlySpan{char})" /> returns the singleton
    /// empty array when the input span is empty.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenSpanIsEmpty_ShouldReturnEmptyArray()
    {
        byte[] result = ReadOnlySpan<char>.Empty.ToUtf8Bytes();

        Assert.AreSame([], result);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeUtf8To(ReadOnlySpan{char}, Span{byte})" /> writes the
    /// expected UTF-8 bytes and returns the correct count.
    /// </summary>
    [TestMethod]
    public void EncodeUtf8To_WhenDestinationFits_ShouldWriteAndReturnCount()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        int written = MultiByteText.AsSpan().EncodeUtf8To(destination);

        Assert.AreEqual(required, written);
        CollectionAssert.AreEqual(System.Text.Encoding.UTF8.GetBytes(MultiByteText), destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeUtf8To(ReadOnlySpan{char}, Span{byte})" /> throws
    /// <see cref="ArgumentException" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void EncodeUtf8To_WhenDestinationIsOneByteTooSmall_ShouldThrowExactly()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        byte[] backing = new byte[required - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = MultiByteText.AsSpan().EncodeUtf8To(backing);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryEncodeUtf8To(ReadOnlySpan{char}, Span{byte}, out int)" />
    /// returns <see langword="true" /> when the destination fits and <see langword="false" /> when it is too
    /// small.
    /// </summary>
    /// <param name="extra">Extra capacity added to the required buffer length (negative to undersize).</param>
    /// <param name="expectedOk">Whether the call is expected to succeed.</param>
    [TestMethod]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(-1, false)]
    public void TryEncodeUtf8To_WhenInvoked_ShouldRespectDestinationSize(int extra, bool expectedOk)
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        byte[] backing = new byte[required + extra];

        bool ok = MultiByteText.AsSpan().TryEncodeUtf8To(backing, out int written);

        Assert.AreEqual(expectedOk, ok);
        Assert.AreEqual(expectedOk ? required : 0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.FromUtf8(ReadOnlySpan{byte})" /> reproduces the original
    /// string after round-tripping through UTF-8 bytes.
    /// </summary>
    /// <param name="key">The pattern key from <see cref="GetPatternText" />.</param>
    [TestMethod]
    [DataRow("empty")]
    [DataRow("ascii")]
    [DataRow("multi-byte")]
    [DataRow("single-emoji")]
    public void FromUtf8_WhenInvoked_ShouldRoundTripWithToUtf8Bytes(string key)
    {
        string text = GetPatternText(key);
        byte[] bytes = text.AsSpan().ToUtf8Bytes();

        string actual = ((ReadOnlySpan<byte>)bytes).FromUtf8();

        Assert.AreEqual(text, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeUtf8To(ReadOnlySpan{byte}, Span{char})" /> writes the
    /// expected characters and returns the correct count.
    /// </summary>
    [TestMethod]
    public void DecodeUtf8To_WhenDestinationFits_ShouldWriteAndReturnCount()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        int required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        int written = ((ReadOnlySpan<byte>)bytes).DecodeUtf8To(destination);

        Assert.AreEqual(required, written);
        Assert.AreEqual(MultiByteText, new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeUtf8To(ReadOnlySpan{byte}, Span{char})" /> throws
    /// <see cref="ArgumentException" /> when the destination is one character too small.
    /// </summary>
    [TestMethod]
    public void DecodeUtf8To_WhenDestinationIsOneCharTooSmall_ShouldThrowExactly()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        int required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        char[] backing = new char[required - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)bytes).DecodeUtf8To(backing);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryDecodeUtf8To(ReadOnlySpan{byte}, Span{char}, out int)" />
    /// returns <see langword="true" /> when the destination fits and <see langword="false" /> when it is too
    /// small.
    /// </summary>
    /// <param name="extra">Extra capacity added to the required buffer length (negative to undersize).</param>
    /// <param name="expectedOk">Whether the call is expected to succeed.</param>
    [TestMethod]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(-1, false)]
    public void TryDecodeUtf8To_WhenInvoked_ShouldRespectDestinationSize(int extra, bool expectedOk)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        int required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        char[] backing = new char[required + extra];

        bool ok = ((ReadOnlySpan<byte>)bytes).TryDecodeUtf8To(backing, out int written);

        Assert.AreEqual(expectedOk, ok);
        Assert.AreEqual(expectedOk ? required : 0, written);
    }
}
