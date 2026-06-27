// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WriteString" /> emits UTF-8 string values.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that the combined <see cref="Utf8BencodeWriter.WriteString(string, string)" /> overload rejects a
    /// null value with <see cref="ArgumentNullException" /> before any byte is emitted.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenCombinedOverloadValueIsNull_ShouldThrowArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteString("name", null!);
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WriteString" /> encodes text as a UTF-8 byte string whose length
    /// prefix counts bytes rather than characters.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenMultibyteText_ShouldEmitUtf8WithByteLength()
    {
        const string Text = "héllo";
        byte[] content = Encoding.UTF8.GetBytes(Text);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        writer.WriteString(Text);

        byte[] expected = [.. Encoding.ASCII.GetBytes($"{content.Length}:"), .. content];
        CollectionAssert.AreEqual(expected, buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WriteString" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>value</c> when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueNull_ShouldThrowArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteString(null!);
        }, "value");
    }

}
