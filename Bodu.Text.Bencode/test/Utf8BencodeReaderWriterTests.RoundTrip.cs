// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderWriterTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that documents written by <see cref="Utf8BencodeWriter" /> read back through
/// <see cref="Utf8BencodeReader" /> with a matching token stream, and that canonical inputs survive a
/// parse-then-rewrite round trip byte-for-byte.
/// </summary>
public partial class Utf8BencodeReaderWriterTests
{
    /// <summary>
    /// Encapsulates a program that drives a <see cref="Utf8BencodeWriter" />, expressed as a named delegate because
    /// the writer is a <see langword="ref struct" /> and therefore cannot be a generic type argument such as
    /// <see cref="Action{T}" />.
    /// </summary>
    /// <param name="writer">The writer to drive.</param>
    public delegate void WriterAction(Utf8BencodeWriter writer);

    /// <summary>
    /// Describes a round-trip scenario: a writer program, the canonical bytes it must emit, and the token sequence
    /// a reader must surface for those bytes.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Write">The callback that drives the writer.</param>
    /// <param name="Expected">The expected canonical encoding, expressed as Latin-1 text.</param>
    /// <param name="Tokens">The token sequence a reader must produce for the encoded bytes.</param>
    public sealed record RoundTripCase(
        string Name,
        WriterAction Write,
        string Expected,
        BencodeTokenType[] Tokens) : IKat;

    /// <summary>
    /// Verifies that the writer emits the documented canonical bytes for each round-trip scenario.
    /// </summary>
    /// <param name="kat">The round-trip known-answer row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Write_WhenDocument_ShouldEmitCanonicalBytes(RoundTripCase kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        kat.Write(writer);

        Assert.AreEqual(kat.Expected, Encoding.Latin1.GetString(buffer.WrittenSpan), kat.Name);
    }

    /// <summary>
    /// Verifies that reading the bytes written for each round-trip scenario reproduces the documented token
    /// sequence.
    /// </summary>
    /// <param name="kat">The round-trip known-answer row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenWrittenThenRead_ShouldProduceExpectedTokens(RoundTripCase kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        kat.Write(writer);
        byte[] bytes = buffer.WrittenSpan.ToArray();

        var reader = new Utf8BencodeReader(bytes);
        var actual = new List<BencodeTokenType>();
        while (reader.Read())
            actual.Add(reader.TokenType);

        CollectionAssert.AreEqual(kat.Tokens, actual, kat.Name);
    }

    /// <summary>
    /// Verifies that parsing a canonical document and re-writing every token through the writer reproduces the
    /// original bytes exactly.
    /// </summary>
    /// <param name="input">The canonical Bencode document, expressed as Latin-1 text.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("i0e")]
    [DataRow("i-42e")]
    [DataRow("i9223372036854775807e")]
    [DataRow("0:")]
    [DataRow("4:spam")]
    [DataRow("le")]
    [DataRow("de")]
    [DataRow("li1e3:abci-2ee")]
    [DataRow("d3:cow3:moo4:spam4:eggse")]
    [DataRow("d8:announce14:http://tracker4:infod4:name8:file.txt12:piece lengthi16384eee")]
    [DataRow("ld1:ai1ee1:be")]
    [DataRow("d1:a2:aae")]
    public void RoundTrip_WhenCanonicalInputRewritten_ShouldBeByteEqual(string input)
    {
        byte[] original = Encoding.Latin1.GetBytes(input);

        byte[] rewritten = Rewrite(original);

        CollectionAssert.AreEqual(original, rewritten);
    }

    /// <summary>
    /// Reads a canonical document and re-emits each token through a fresh writer, reproducing the document.
    /// </summary>
    /// <param name="source">The canonical source bytes.</param>
    /// <returns>The re-emitted bytes.</returns>
    private static byte[] Rewrite(ReadOnlySpan<byte> source)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        var reader = new Utf8BencodeReader(source);

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case BencodeTokenType.StartList:
                    writer.WriteStartList();
                    break;

                case BencodeTokenType.EndList:
                    writer.WriteEndList();
                    break;

                case BencodeTokenType.StartDictionary:
                    writer.WriteStartDictionary();
                    break;

                case BencodeTokenType.EndDictionary:
                    writer.WriteEndDictionary();
                    break;

                case BencodeTokenType.PropertyName:
                    writer.WritePropertyName(reader.ValueSpan);
                    break;

                case BencodeTokenType.Integer:
                    writer.WriteInteger(reader.GetInt64());
                    break;

                case BencodeTokenType.ByteString:
                    writer.WriteByteString(reader.ValueSpan);
                    break;

                default:
                    Assert.Fail($"Unexpected token {reader.TokenType}.");
                    break;
            }
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Provides round-trip scenarios spanning scalars, lists of dictionaries, and nested torrent-like documents.
    /// </summary>
    /// <returns>The round-trip rows.</returns>
    public static IEnumerable<object[]> RoundTripCases()
    {
        yield return Row(new RoundTripCase(
            "integer",
            w => w.WriteInteger(-42),
            "i-42e",
            [BencodeTokenType.Integer]));

        yield return Row(new RoundTripCase(
            "byte string",
            w => w.WriteString("spam"),
            "4:spam",
            [BencodeTokenType.ByteString]));

        yield return Row(new RoundTripCase(
            "list of scalars",
            w =>
            {
                w.WriteStartList();
                w.WriteInteger(1);
                w.WriteString("abc");
                w.WriteInteger(-2);
                w.WriteEndList();
            },
            "li1e3:abci-2ee",
            [
                BencodeTokenType.StartList,
                BencodeTokenType.Integer,
                BencodeTokenType.ByteString,
                BencodeTokenType.Integer,
                BencodeTokenType.EndList,
            ]));

        yield return Row(new RoundTripCase(
            "list of dictionaries",
            w =>
            {
                w.WriteStartList();
                w.WriteStartDictionary();
                w.WritePropertyName("a");
                w.WriteInteger(1);
                w.WriteEndDictionary();
                w.WriteStartDictionary();
                w.WritePropertyName("b");
                w.WriteInteger(2);
                w.WriteEndDictionary();
                w.WriteEndList();
            },
            "ld1:ai1eed1:bi2eee",
            [
                BencodeTokenType.StartList,
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.Integer,
                BencodeTokenType.EndDictionary,
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.Integer,
                BencodeTokenType.EndDictionary,
                BencodeTokenType.EndList,
            ]));

        yield return Row(new RoundTripCase(
            "torrent-like dictionary",
            w =>
            {
                w.WriteStartDictionary();
                w.WritePropertyName("announce");
                w.WriteString("http://tracker");
                w.WritePropertyName("info");
                w.WriteStartDictionary();
                w.WritePropertyName("name");
                w.WriteString("file.txt");
                w.WritePropertyName("piece length");
                w.WriteInteger(16384);
                w.WriteEndDictionary();
                w.WriteEndDictionary();
            },
            "d8:announce14:http://tracker4:infod4:name8:file.txt12:piece lengthi16384eee",
            [
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.ByteString,
                BencodeTokenType.PropertyName,
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.ByteString,
                BencodeTokenType.PropertyName,
                BencodeTokenType.Integer,
                BencodeTokenType.EndDictionary,
                BencodeTokenType.EndDictionary,
            ]));

        static object[] Row(RoundTripCase kat) => [kat];
    }
}
