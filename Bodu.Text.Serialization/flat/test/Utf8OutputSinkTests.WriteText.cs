// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8OutputSinkTests.WriteText.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8OutputSinkTests
{
    /// <summary>
    /// Verifies that text is written as its UTF-8 bytes, characters outside the Basic Multilingual Plane included, and
    /// that the count is of bytes rather than characters.
    /// </summary>
    /// <param name="text">The text to write.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("plain ASCII")]
    [DataRow("café ☕")]
    [DataRow("\U0001F600 and ÿ")]
    public void WriteText_WhenTextIsWritten_ShouldWriteItsUtf8Bytes(string text)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var sink = new Utf8OutputSink(buffer);

        sink.WriteText(text);

        byte[] expected = Encoding.UTF8.GetBytes(text);
        CollectionAssert.AreEqual(expected, buffer.WrittenSpan.ToArray());
        Assert.AreEqual((long)expected.Length, sink.BytesCommitted);
    }

    /// <summary>
    /// Verifies that text with an unpaired surrogate is written with U+FFFD in its place, as <see cref="Encoding.UTF8" />
    /// encodes it.
    /// </summary>
    [TestMethod]
    public void WriteText_WhenTextHoldsAnUnpairedSurrogate_ShouldWriteAReplacementCharacter()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var sink = new Utf8OutputSink(buffer);

        sink.WriteText("a\uD800b");

        CollectionAssert.AreEqual(new byte[] { (byte)'a', 0xEF, 0xBF, 0xBD, (byte)'b' }, buffer.WrittenSpan.ToArray());
        Assert.AreEqual(5L, sink.BytesCommitted);
    }
}
