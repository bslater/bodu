// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WritePropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WritePropertyName" /> emits dictionary keys.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that writing a property name when no container is open throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNoContainerOpen_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WritePropertyName("a");
        });
    }

    /// <summary>
    /// Verifies that writing a property name while the current container is a list throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenCurrentContainerIsList_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartList();
            writer.WritePropertyName("a");
        });
    }

    /// <summary>
    /// Verifies that writing a second property name while the first is still awaiting its value throws
    /// <see cref="InvalidOperationException" /> instead of silently discarding the first key.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenPropertyNamePending_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("a");
            writer.WritePropertyName("b");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WritePropertyName(ReadOnlySpan{char})" /> encodes the characters
    /// as UTF-8, matching the <see langword="string" /> overload byte for byte.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNameIsCharSpan_ShouldEncodeAsUtf8()
    {
        string actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("café".AsSpan());
            w.WriteInteger(1);
            w.WriteEndDictionary();
        });

        string expected = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("café");
            w.WriteInteger(1);
            w.WriteEndDictionary();
        });

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.WritePropertyName(string)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>name</c> when the name is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNameNull_ShouldThrowArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName((string)null!);
        }, "name");
    }

}
