// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.Transcode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that the Encoding-receiver <c>Transcode</c> delegates to the span-receiver implementation and
    /// returns identical bytes.
    /// </summary>
    [TestMethod]
    public void EncodingReceiver_Transcode_ShouldMatchSpanReceiver()
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        byte[] expected = ((ReadOnlySpan<byte>)utf8).Transcode(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode);

        byte[] actual = System.Text.Encoding.UTF8.Transcode(utf8, System.Text.Encoding.Unicode);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetTranscodedByteCount(System.Text.Encoding, ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// returns the exact destination byte count.
    /// </summary>
    [TestMethod]
    public void GetTranscodedByteCount_WhenInvoked_ShouldReturnExactBytesOfDestinationEncoding()
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        int expected = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode, utf8).Length;

        int actual = System.Text.Encoding.UTF8.GetTranscodedByteCount(utf8, System.Text.Encoding.Unicode);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetTranscodedByteCount(System.Text.Encoding, ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// returns zero for an empty input span.
    /// </summary>
    [TestMethod]
    public void GetTranscodedByteCount_WhenInputIsEmpty_ShouldReturnZero()
    {
        int actual = System.Text.Encoding.UTF8.GetTranscodedByteCount([], System.Text.Encoding.Unicode);

        Assert.AreEqual(0, actual);
    }

    /// <summary>
    /// Verifies that the Encoding-receiver <c>TranscodeTo</c> overload writes the same bytes as the span receiver.
    /// </summary>
    [TestMethod]
    public void EncodingReceiver_TranscodeTo_ShouldMatchSpanReceiver()
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        int required = System.Text.Encoding.UTF8.GetTranscodedByteCount(utf8, System.Text.Encoding.Unicode);
        byte[] expected = ((ReadOnlySpan<byte>)utf8).Transcode(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode);
        Span<byte> destination = new byte[required];

        int written = System.Text.Encoding.UTF8.TranscodeTo(utf8, System.Text.Encoding.Unicode, destination);

        Assert.AreEqual(required, written);
        CollectionAssert.AreEqual(expected, destination.ToArray());
    }

    /// <summary>
    /// Verifies that the Encoding-receiver <c>TryTranscodeTo</c> overload mirrors the span receiver outcome.
    /// </summary>
    [TestMethod]
    public void EncodingReceiver_TryTranscodeTo_ShouldMatchSpanReceiver()
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        int required = System.Text.Encoding.UTF8.GetTranscodedByteCount(utf8, System.Text.Encoding.Unicode);
        byte[] backing = new byte[required];

        bool ok = System.Text.Encoding.UTF8.TryTranscodeTo(utf8, System.Text.Encoding.Unicode, backing, out int written);

        Assert.IsTrue(ok);
        Assert.AreEqual(required, written);
    }

    /// <summary>
    /// Verifies that the Encoding-receiver Transcode methods all throw <see cref="ArgumentNullException" /> when
    /// either encoding is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncodingReceiver_Transcode_WhenAnyEncodingIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.Transcode(null!, [0x68], System.Text.Encoding.Unicode));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = System.Text.Encoding.UTF8.Transcode([0x68], null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.GetTranscodedByteCount(null!, [0x68], System.Text.Encoding.Unicode));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = System.Text.Encoding.UTF8.GetTranscodedByteCount([0x68], null!));
    }
}
