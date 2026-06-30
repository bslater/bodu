// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteStartDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WriteStartDictionary" /> opens a dictionary container.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that opening a dictionary directly inside a dictionary, where a property name is expected, throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteStartDictionary_WhenPropertyNameExpected_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteStartDictionary();
        });
    }

    /// <summary>
    /// Verifies that an empty dictionary is emitted as <c>de</c>.
    /// </summary>
    [TestMethod]
    public void WriteStartDictionary_WhenEmpty_ShouldEmitEmptyDictionary()
    {
        string actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WriteEndDictionary();
        });

        Assert.AreEqual("de", actual);
    }

}
