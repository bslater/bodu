// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the low-level <see cref="Utf8BencodeReader" /> and <see cref="Utf8BencodeWriter" /> ref-struct token types.
/// </summary>
[TestClass]
public class Utf8BencodeReaderWriterTests
{
    /// <summary>
    /// Verifies that the writer emits canonical bytes for scalars and that the reader reads them back.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Writer_WhenWritingScalars_ShouldEmitCanonicalBytesReadableByReader()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        writer.WriteInteger(42);

        Assert.AreEqual("i42e", Encoding.Latin1.GetString(buffer.WrittenSpan));

        var reader = new Utf8BencodeReader(buffer.WrittenSpan);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(42, reader.GetInt64());
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that the writer sorts dictionary keys into canonical ascending order.
    /// </summary>
    [TestMethod]
    public void Writer_WhenWritingDictionary_ShouldSortKeys()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        writer.WriteStartDictionary();
        writer.WritePropertyName("name");
        writer.WriteString("x");
        writer.WritePropertyName("Length");
        writer.WriteInteger(5);
        writer.WriteEndDictionary();

        Assert.AreEqual("d6:Lengthi5e4:name1:xe", Encoding.Latin1.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that the reader surfaces a dictionary as start, property names, values, and end tokens.
    /// </summary>
    [TestMethod]
    public void Reader_WhenReadingDictionary_ShouldSurfaceKeyAndValueTokens()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d3:cow3:moo4:spam4:eggse");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartDictionary, reader.TokenType);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("cow", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("moo", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("spam", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a round trip of a nested list through the writer and reader preserves structure.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenNestedList_ShouldPreserveStructure()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        writer.WriteStartList();
        writer.WriteInteger(1);
        writer.WriteString("two");
        writer.WriteEndList();

        Assert.AreEqual("li1e3:twoe", Encoding.Latin1.GetString(buffer.WrittenSpan));

        var reader = new Utf8BencodeReader(buffer.WrittenSpan);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.GetInt64());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("two", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
    }

    /// <summary>
    /// Verifies that the reader rejects malformed input.
    /// </summary>
    /// <param name="input">The malformed Bencode, expressed as Latin-1 text.</param>
    [TestMethod]
    [DataRow("i03e")]
    [DataRow("i-0e")]
    [DataRow("d1:b1:v1:a1:we")]
    [DataRow("3abc")]
    [DataRow("le2:xx")]
    public void Reader_WhenInputMalformed_ShouldThrowBencodeFormatException(string input)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(input);

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            while (reader.Read())
            {
                // Drive the reader to completion to surface any malformed-input error.
            }
        });
    }
}
