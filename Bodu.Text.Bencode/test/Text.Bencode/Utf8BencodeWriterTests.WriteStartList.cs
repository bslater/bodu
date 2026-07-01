// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteStartList.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.WriteStartList" /> opens a list container.
/// </summary>
public partial class Utf8BencodeWriterTests
{
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
    /// Verifies that an empty list is emitted as <c>le</c>.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenEmpty_ShouldEmitEmptyList()
    {
        string actual = Write(w =>
        {
            w.WriteStartList();
            w.WriteEndList();
        });

        Assert.AreEqual("le", actual);
    }

    /// <summary>
    /// Verifies that a list preserves element insertion order and mixes value kinds.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenMixedValues_ShouldPreserveOrder()
    {
        string actual = Write(w =>
        {
            w.WriteStartList();
            w.WriteInteger(1);
            w.WriteString("two");
            w.WriteInteger(-3);
            w.WriteEndList();
        });

        Assert.AreEqual("li1e3:twoi-3ee", actual);
    }

    /// <summary>
    /// Verifies that nested lists are emitted with balanced delimiters in insertion order.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenNested_ShouldEmitBalancedDelimiters()
    {
        string actual = Write(w =>
        {
            w.WriteStartList();
            w.WriteStartList();
            w.WriteInteger(1);
            w.WriteEndList();
            w.WriteStartList();
            w.WriteEndList();
            w.WriteEndList();
        });

        Assert.AreEqual("lli1eelee", actual);
    }

    /// <summary>
    /// Verifies that opening a container after a complete root value throws
    /// <see cref="InvalidOperationException" />, confirming the single-root rule applies to containers as well as
    /// scalars.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenRootValueAlreadyComplete_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteEndDictionary();
            writer.WriteStartList();
        });
    }

    /// <summary>
    /// Verifies that opening more containers than the configured maximum depth allows throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenExceedingMaxDepth_ShouldThrowBencodeSerializationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = 2 });
            writer.WriteStartList();
            writer.WriteStartList();
            writer.WriteStartList();
        });
    }

    /// <summary>
    /// Verifies that opening containers up to exactly the configured maximum depth is permitted.
    /// </summary>
    [TestMethod]
    public void WriteStartList_WhenAtMaxDepth_ShouldNotThrow()
    {
        string actual = Write(() => new BencodeWriterOptions { MaxDepth = 2 }, w =>
        {
            w.WriteStartList();
            w.WriteStartList();
            w.WriteEndList();
            w.WriteEndList();
        });

        Assert.AreEqual("llee", actual);
    }

}
