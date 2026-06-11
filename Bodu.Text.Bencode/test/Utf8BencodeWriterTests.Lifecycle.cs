// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.Lifecycle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter" /> rejects call sequences that would emit non-canonical Bencode —
/// duplicate dictionary keys, values without a property name, property names outside a dictionary, and mismatched
/// container ends — with <see cref="BencodeSerializationException" /> or <see cref="InvalidOperationException" />.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that closing a dictionary containing two entries with the same key throws
    /// <see cref="BencodeSerializationException" />, because canonical Bencode (BEP 3) forbids duplicate keys.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateKeys_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("a");
            writer.WriteInteger(1);
            writer.WritePropertyName("a");
            writer.WriteInteger(2);
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.CurrentDepth" /> tracks container opens and closes, returning to
    /// zero when the document completes — the completeness assertion available to manual writer callers.
    /// </summary>
    [TestMethod]
    public void CurrentDepth_WhenContainersOpenAndClose_ShouldTrackNesting()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        Assert.AreEqual(0, writer.CurrentDepth);

        writer.WriteStartDictionary();
        Assert.AreEqual(1, writer.CurrentDepth);

        writer.WritePropertyName("a");
        writer.WriteStartList();
        Assert.AreEqual(2, writer.CurrentDepth);

        writer.WriteInteger(1);
        writer.WriteEndList();
        Assert.AreEqual(1, writer.CurrentDepth);

        writer.WriteEndDictionary();
        Assert.AreEqual(0, writer.CurrentDepth);
    }

    /// <summary>
    /// Verifies that a duplicate-key failure on a root dictionary leaves the destination untouched: validation must
    /// complete before any byte of the dictionary is emitted, so a failed close does not leak a partial document.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateKeysAtRoot_ShouldEmitNothing()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("a");
            writer.WriteInteger(1);
            writer.WritePropertyName("b");
            writer.WriteInteger(2);
            writer.WritePropertyName("b");
            writer.WriteInteger(3);
            writer.WriteEndDictionary();
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that closing a dictionary whose duplicate keys are separated by another entry throws
    /// <see cref="BencodeSerializationException" />, confirming duplicates are detected after the canonical sort
    /// rather than only between adjacent writes.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateKeysNotAdjacent_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("b");
            writer.WriteInteger(1);
            writer.WritePropertyName("a");
            writer.WriteInteger(2);
            writer.WritePropertyName("b");
            writer.WriteInteger(3);
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that closing a dictionary containing two entries with the same non-UTF-8 binary key throws
    /// <see cref="BencodeSerializationException" />, confirming duplicate detection compares raw key bytes.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenDuplicateBinaryKeys_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();
        byte[] key = [0xFF, 0x00, 0x80];

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName(key);
            writer.WriteInteger(1);
            writer.WritePropertyName(key);
            writer.WriteInteger(2);
            writer.WriteEndDictionary();
        });
    }

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
    /// Verifies that writing an integer directly inside a dictionary, where a property name is expected, throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenPropertyNameExpected_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteInteger(1);
        });
    }

    /// <summary>
    /// Verifies that writing a byte string directly inside a dictionary, where a property name is expected, throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteByteString_WhenPropertyNameExpected_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteByteString("x"u8);
        });
    }

    /// <summary>
    /// Verifies that opening a list directly inside a dictionary, where a property name is expected, throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenPropertyNameExpected_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteStartList();
        });
    }

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
    /// Verifies that closing a dictionary while a property name is still awaiting its value throws
    /// <see cref="InvalidOperationException" /> instead of silently dropping the dangling key.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenPropertyNamePending_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WritePropertyName("a");
            writer.WriteEndDictionary();
        });
    }

    /// <summary>
    /// Verifies that closing the current container as a list while it is a dictionary throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteEndList_WhenCurrentContainerIsDictionary_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteEndList();
        });
    }

    /// <summary>
    /// Verifies that closing the current container as a dictionary while it is a list throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteEndDictionary_WhenCurrentContainerIsList_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartList();
            writer.WriteEndDictionary();
        });
    }
}
