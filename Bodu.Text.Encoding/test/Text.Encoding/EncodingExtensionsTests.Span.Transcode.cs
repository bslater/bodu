// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Span.Transcode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.Transcode(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding)" />
    /// performs a UTF-8 → UTF-16 round trip whose bytes match the BCL <see cref="System.Text.Encoding.Convert" />.
    /// </summary>
    [TestMethod]
    public void Transcode_FromUtf8ToUtf16_ShouldMatchBclConvert()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var expected = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode, utf8);

        var actual = ((ReadOnlySpan<byte>)utf8).Transcode(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.Transcode(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding)" />
    /// is the identity when source and destination encodings agree.
    /// </summary>
    [TestMethod]
    public void Transcode_WhenSourceAndDestinationMatch_ShouldRoundTripExactly()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        var actual = ((ReadOnlySpan<byte>)utf8).Transcode(System.Text.Encoding.UTF8, System.Text.Encoding.UTF8);

        CollectionAssert.AreEqual(utf8, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.Transcode(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding)" />
    /// returns the singleton empty array for an empty input span.
    /// </summary>
    [TestMethod]
    public void Transcode_WhenSourceIsEmpty_ShouldReturnEmptyArray()
    {
        var actual = ReadOnlySpan<byte>.Empty.Transcode(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode);

        Assert.AreSame(Array.Empty<byte>(), actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.Transcode(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>sourceEncoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transcode_WhenSourceEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).Transcode(null!, System.Text.Encoding.Unicode);
        });

        Assert.AreEqual("sourceEncoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.Transcode(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>destinationEncoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transcode_WhenDestinationEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).Transcode(System.Text.Encoding.UTF8, null!);
        });

        Assert.AreEqual("destinationEncoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TranscodeTo(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding, Span{byte})" />
    /// writes the expected bytes when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void TranscodeTo_WhenDestinationFitsExactly_ShouldWriteAndReturnCount()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var expected = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode, utf8);
        Span<byte> destination = new byte[expected.Length];

        var written = ((ReadOnlySpan<byte>)utf8).TranscodeTo(
            System.Text.Encoding.UTF8,
            System.Text.Encoding.Unicode,
            destination);

        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TranscodeTo(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding, Span{byte})" />
    /// returns zero when the input span is empty.
    /// </summary>
    [TestMethod]
    public void TranscodeTo_WhenSourceIsEmpty_ShouldReturnZero()
    {
        var backing = new byte[16];

        var written = ReadOnlySpan<byte>.Empty.TranscodeTo(
            System.Text.Encoding.UTF8,
            System.Text.Encoding.Unicode,
            backing);

        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TranscodeTo(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TranscodeTo_WhenDestinationIsTooSmall_ShouldThrowExactly()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode, utf8).Length;
        var backing = new byte[required - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)utf8).TranscodeTo(
                System.Text.Encoding.UTF8,
                System.Text.Encoding.Unicode,
                backing);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TranscodeTo(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentNullException" /> for null encodings.
    /// </summary>
    [TestMethod]
    public void TranscodeTo_WhenSourceEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new byte[16];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).TranscodeTo(null!, System.Text.Encoding.Unicode, backing);
        });

        Assert.AreEqual("sourceEncoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryTranscodeTo(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="true" /> and the byte count when the destination fits.
    /// </summary>
    [TestMethod]
    public void TryTranscodeTo_WhenDestinationFits_ShouldReturnTrueAndReportCount()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var expected = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode, utf8);
        Span<byte> destination = new byte[expected.Length + 4];

        var ok = ((ReadOnlySpan<byte>)utf8).TryTranscodeTo(
            System.Text.Encoding.UTF8,
            System.Text.Encoding.Unicode,
            destination,
            out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(expected.Length, bytesWritten);
        CollectionAssert.AreEqual(expected, destination.Slice(0, bytesWritten).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryTranscodeTo(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryTranscodeTo_WhenDestinationIsTooSmall_ShouldReturnFalse()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var backing = new byte[1];

        var ok = ((ReadOnlySpan<byte>)utf8).TryTranscodeTo(
            System.Text.Encoding.UTF8,
            System.Text.Encoding.Unicode,
            backing,
            out var written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryTranscodeTo(ReadOnlySpan{byte}, System.Text.Encoding, System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="true" /> with zero bytes written for an empty input span.
    /// </summary>
    [TestMethod]
    public void TryTranscodeTo_WhenSourceIsEmpty_ShouldReturnTrueWithZeroBytesWritten()
    {
        var backing = new byte[8];

        var ok = ReadOnlySpan<byte>.Empty.TryTranscodeTo(
            System.Text.Encoding.UTF8,
            System.Text.Encoding.Unicode,
            backing,
            out var written);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, written);
    }
}
