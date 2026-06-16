// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeReader" /> surfaces every Bencode token shape with the correct
/// <see cref="BencodeTokenType" /> sequence, decoded values, depth, and byte-consumption progression.
/// </summary>
[TestClass]
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Decodes the supplied Latin-1 text to bytes so binary content survives unchanged.
    /// </summary>
    /// <param name="text">The Latin-1 text to decode.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Bytes(string text) => Encoding.Latin1.GetBytes(text);

    /// <summary>
    /// Verifies that reading a single integer token reports the <see cref="BencodeTokenType.Integer" /> token, the
    /// decoded value, and that a second read reports end of document.
    /// </summary>
    [TestMethod]
    public void Read_WhenSingleInteger_ShouldReportIntegerTokenThenEnd()
    {
        byte[] bytes = Bytes("i42e");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(42L, reader.GetInt64());
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.AreEqual(4, reader.BytesConsumed);

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
        Assert.AreEqual(4, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that the reader decodes the full range of canonical integer values, including zero, one, negative
    /// one, and the 64-bit extremes.
    /// </summary>
    /// <param name="text">The Bencode integer token, expressed as Latin-1 text.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [DataRow("i0e", 0L)]
    [DataRow("i1e", 1L)]
    [DataRow("i-1e", -1L)]
    [DataRow("i123456789e", 123456789L)]
    [DataRow("i-123456789e", -123456789L)]
    [DataRow("i9223372036854775807e", long.MaxValue)]
    [DataRow("i-9223372036854775808e", long.MinValue)]
    public void Read_WhenInteger_ShouldDecodeValue(string text, long expected)
    {
        byte[] bytes = Bytes(text);
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(expected, reader.GetInt64());
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that reading an empty byte string (<c>0:</c>) reports a zero-length
    /// <see cref="BencodeTokenType.ByteString" /> token.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyByteString_ShouldReportZeroLengthValue()
    {
        byte[] bytes = Bytes("0:");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual(0, reader.ValueSpan.Length);
        Assert.AreEqual(string.Empty, reader.GetString());
        Assert.AreEqual(0, reader.GetBytes().Length);
        Assert.AreEqual(2, reader.BytesConsumed);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that reading an ASCII byte string reports its content through
    /// <see cref="Utf8BencodeReader.GetString" /> and <see cref="Utf8BencodeReader.ValueSpan" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenAsciiByteString_ShouldReportContent()
    {
        byte[] bytes = Bytes("4:spam");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("spam", reader.GetString());
        CollectionAssert.AreEqual(Bytes("spam"), reader.ValueSpan.ToArray());
        Assert.AreEqual(6, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a byte string carrying multibyte UTF-8 content decodes correctly, with the length prefix
    /// counting bytes rather than characters.
    /// </summary>
    [TestMethod]
    public void Read_WhenUtf8MultibyteByteString_ShouldDecodeText()
    {
        const string Text = "hélloé中";
        byte[] content = Encoding.UTF8.GetBytes(Text);
        byte[] bytes = [.. Encoding.ASCII.GetBytes($"{content.Length}:"), .. content];
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual(Text, reader.GetString());
        CollectionAssert.AreEqual(content, reader.GetBytes());
    }

    /// <summary>
    /// Verifies that a byte string carrying non-UTF-8 binary content is preserved verbatim through
    /// <see cref="Utf8BencodeReader.GetBytes" /> and <see cref="Utf8BencodeReader.ValueSpan" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenBinaryByteString_ShouldPreserveBytes()
    {
        byte[] content = [0x00, 0xFF, 0x80, 0x01, 0xFE, 0x7F];
        byte[] bytes = [.. Encoding.ASCII.GetBytes($"{content.Length}:"), .. content];
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        CollectionAssert.AreEqual(content, reader.GetBytes());
        CollectionAssert.AreEqual(content, reader.ValueSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetString" /> on a byte string carrying non-UTF-8 binary content
    /// decodes with U+FFFD replacement characters rather than throwing, pinning the documented contract that
    /// <see cref="Utf8BencodeReader.GetBytes" /> is the lossless accessor for binary content.
    /// </summary>
    [TestMethod]
    public void GetString_WhenContentNotValidUtf8_ShouldDecodeWithReplacementCharacters()
    {
        // 0xC3 starts a two-byte UTF-8 sequence, but 0x28 ('(') is not a valid continuation byte.
        byte[] bytes = [(byte)'2', (byte)':', 0xC3, 0x28];
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("�(", reader.GetString());
        CollectionAssert.AreEqual(new byte[] { 0xC3, 0x28 }, reader.GetBytes());
    }

    /// <summary>
    /// Verifies that a long byte string whose length prefix consumes the entire remaining buffer is read in full.
    /// </summary>
    [TestMethod]
    public void Read_WhenLongByteString_ShouldReadEntireContent()
    {
        byte[] content = new byte[5000];
        for (int i = 0; i < content.Length; i++)
            content[i] = (byte)(i % 251);

        byte[] bytes = [.. Encoding.ASCII.GetBytes($"{content.Length}:"), .. content];
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual(content.Length, reader.ValueSpan.Length);
        CollectionAssert.AreEqual(content, reader.GetBytes());
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an empty list (<c>le</c>) reports a start and end token with the depth rising to one and
    /// falling back to zero.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyList_ShouldReportStartThenEnd()
    {
        byte[] bytes = Bytes("le");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);
        Assert.AreEqual(1, reader.BytesConsumed);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.AreEqual(2, reader.BytesConsumed);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a list of scalar values reports each element in order with the surrounding start and end
    /// tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenListOfScalars_ShouldReportElementsInOrder()
    {
        byte[] bytes = Bytes("li1e3:abci-2ee");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(1L, reader.GetInt64());
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("abc", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(-2L, reader.GetInt64());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that nested lists raise and lower the reported depth as containers open and close.
    /// </summary>
    [TestMethod]
    public void Read_WhenNestedLists_ShouldTrackDepth()
    {
        byte[] bytes = Bytes("lli1eee");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);
        Assert.AreEqual(2, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(2, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an empty dictionary (<c>de</c>) reports a start and end token with the depth rising to one and
    /// falling back to zero.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDictionary_ShouldReportStartThenEnd()
    {
        byte[] bytes = Bytes("de");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartDictionary, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a single-entry dictionary reports the property name and value as distinct tokens and exposes
    /// the key through <see cref="Utf8BencodeReader.GetString" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSingleEntryDictionary_ShouldReportPropertyNameAndValue()
    {
        byte[] bytes = Bytes("d3:cow3:mooe");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartDictionary, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("cow", reader.GetString());
        CollectionAssert.AreEqual(Bytes("cow"), reader.GetBytes());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("moo", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a dictionary holding a value of every supported kind reports the expected token sequence and
    /// decoded values.
    /// </summary>
    [TestMethod]
    public void Read_WhenDictionaryWithMixedValues_ShouldReportEachValueKind()
    {
        // Keys in canonical ascending order: dct, int, lst, str.
        byte[] bytes = Bytes("d3:dctde3:inti7e3:lstle3:str3:abce");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartDictionary, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("dct", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartDictionary, reader.TokenType);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("int", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(7L, reader.GetInt64());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("lst", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("str", reader.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("abc", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a dictionary nested inside another dictionary tracks depth and reports both containers'
    /// boundaries.
    /// </summary>
    [TestMethod]
    public void Read_WhenNestedDictionaries_ShouldTrackDepth()
    {
        byte[] bytes = Bytes("d1:ad1:bi1eee");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartDictionary, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("a", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartDictionary, reader.TokenType);
        Assert.AreEqual(2, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("b", reader.GetString());
        Assert.AreEqual(2, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.BytesConsumed" /> advances to the exact end offset of each token
    /// as the reader walks a document.
    /// </summary>
    [TestMethod]
    public void Read_WhenWalkingDocument_ShouldReportBytesConsumedAtEachToken()
    {
        byte[] bytes = Bytes("d3:cow3:moo4:spam4:eggse");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.BytesConsumed);  // d

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(6, reader.BytesConsumed);  // 3:cow

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(11, reader.BytesConsumed); // 3:moo

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(17, reader.BytesConsumed); // 4:spam

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(23, reader.BytesConsumed); // 4:eggs

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(24, reader.BytesConsumed); // e

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(24, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that the reader reports the expected token-type sequence for a representative set of documents.
    /// </summary>
    /// <param name="kat">The token-sequence known-answer row.</param>
    [TestMethod]
    [DynamicData(
        nameof(TokenSequenceCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenDocument_ShouldProduceExpectedTokenSequence(ValidKat<string, BencodeTokenType[]> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = Bytes(kat.Input);
        var reader = new Utf8BencodeReader(bytes);

        var actual = new List<BencodeTokenType>();
        while (reader.Read())
            actual.Add(reader.TokenType);

        CollectionAssert.AreEqual(kat.Expected, actual, kat.Name);
        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.Read" /> after the document has ended continues to return
    /// <see langword="false" /> and leaves the token type at <see cref="BencodeTokenType.None" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenCalledRepeatedlyAfterEnd_ShouldKeepReturningFalse()
    {
        byte[] bytes = Bytes("i1e");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
    }

    /// <summary>
    /// Verifies that an empty input yields no tokens and reports <see cref="BencodeTokenType.None" /> without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Read_WhenInputEmpty_ShouldReturnFalse()
    {
        var reader = new Utf8BencodeReader([]);

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
        Assert.AreEqual(0, reader.BytesConsumed);
        Assert.AreEqual(0, reader.CurrentDepth);
    }

    /// <summary>
    /// Verifies that the token type is <see cref="BencodeTokenType.None" /> before the first read.
    /// </summary>
    [TestMethod]
    public void TokenType_WhenBeforeFirstRead_ShouldBeNone()
    {
        byte[] bytes = Bytes("i1e");
        var reader = new Utf8BencodeReader(bytes);

        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.AreEqual(0, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetInt64" /> on a byte-string token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenTokenIsByteString_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("3:abc");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetInt64" /> on a container-start token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenTokenIsStartList_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("le");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetInt64" /> before any token is read throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenTokenIsNone_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i1e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetString" /> on an integer token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsInteger_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i5e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetString();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetBytes" /> on an integer token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenTokenIsInteger_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i5e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            _ = reader.GetBytes();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8BencodeReader.GetString" /> on an end-of-document position throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsNone_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Bytes("i5e");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            reader.Read();
            reader.Read();
            _ = reader.GetString();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetString" /> and <see cref="Utf8BencodeReader.GetBytes" /> both
    /// read a property-name token, since a key is a byte string in key position.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsPropertyName_ShouldReturnKey()
    {
        byte[] bytes = Bytes("d3:cow3:mooe");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());

        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("cow", reader.GetString());
        CollectionAssert.AreEqual(Bytes("cow"), reader.GetBytes());
    }

    /// <summary>
    /// Provides token-sequence known-answer rows covering scalars, empty and populated containers, and nesting.
    /// </summary>
    /// <returns>The token-sequence rows.</returns>
    public static IEnumerable<object[]> TokenSequenceCases()
    {
        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "integer",
            "i1e",
            [BencodeTokenType.Integer]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "byte string",
            "3:abc",
            [BencodeTokenType.ByteString]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "empty list",
            "le",
            [BencodeTokenType.StartList, BencodeTokenType.EndList]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "empty dictionary",
            "de",
            [BencodeTokenType.StartDictionary, BencodeTokenType.EndDictionary]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "list of scalars",
            "li1e3:abce",
            [BencodeTokenType.StartList, BencodeTokenType.Integer, BencodeTokenType.ByteString, BencodeTokenType.EndList]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "single-entry dictionary",
            "d3:cow3:mooe",
            [
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.ByteString,
                BencodeTokenType.EndDictionary,
            ]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "nested list",
            "lli1eee",
            [
                BencodeTokenType.StartList,
                BencodeTokenType.StartList,
                BencodeTokenType.Integer,
                BencodeTokenType.EndList,
                BencodeTokenType.EndList,
            ]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "dictionary with list value",
            "d4:listli1ei2eee",
            [
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.StartList,
                BencodeTokenType.Integer,
                BencodeTokenType.Integer,
                BencodeTokenType.EndList,
                BencodeTokenType.EndDictionary,
            ]));

        static object[] Row(ValidKat<string, BencodeTokenType[]> kat) => [kat];
    }
}
