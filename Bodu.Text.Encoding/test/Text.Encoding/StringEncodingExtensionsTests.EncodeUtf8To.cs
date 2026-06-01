// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.EncodeUtf8To.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.EncodeUtf8To(string, Span{byte})" /> writes the
    /// expected UTF-8 bytes and returns the correct count.
    /// </summary>
    [TestMethod]
    public void EncodeUtf8To_WhenDestinationFits_ShouldWriteAndReturnCount()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        var written = MultiByteText.EncodeUtf8To(destination);

        Assert.AreEqual(required, written);
        CollectionAssert.AreEqual(System.Text.Encoding.UTF8.GetBytes(MultiByteText), destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.EncodeUtf8To(string, Span{byte})" /> throws
    /// <see cref="ArgumentException" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void EncodeUtf8To_WhenDestinationIsOneByteTooSmall_ShouldThrowExactly()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        var backing = new byte[required - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = MultiByteText.EncodeUtf8To(backing);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.EncodeUtf8To(string, Span{byte})" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncodeUtf8To_WhenTextIsNull_ShouldThrowExactly()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.EncodeUtf8To(null!, backing);
        });

        Assert.AreEqual("text", ex.ParamName);
    }
}
