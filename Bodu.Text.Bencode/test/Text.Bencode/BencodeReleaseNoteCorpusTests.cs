// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;

using Bodu.Test.Corpus;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Holds Bodu.Text.Bencode to the defect fixes other bencode libraries list in their release notes, as catalogued in
/// <c>corpus/bencode/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A read is rendered as a token transcript: the tokens in order, separated by single spaces. <c>[</c> and <c>]</c>
/// start and end a list, <c>{</c> and <c>}</c> a dictionary, <c>k:</c> introduces a key, <c>s:</c> a byte string and
/// <c>i:</c> an integer's text. Each token's bytes use the catalogue escapes, with a space written <c>\x20</c>, and
/// transcripts are compared after each token is decoded and encoded again, so equivalent escapes compare equal.
/// </para>
/// <para>
/// The <c>Surface</c> option selects what reads the input: <see cref="Utf8BencodeReader" /> (the default),
/// <see cref="BencodeDocument" />, or the mutable <see cref="BencodeNode" /> tree, whose dictionaries render in
/// ascending bytewise key order, the order its writer uses. A write row replays a transcript as calls on
/// <see cref="Utf8BencodeWriter" />, and a round-trip row copies the input token by token from the reader to the writer.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "bencode";

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames =
    [
        "Surface", "MaxDepth", "AllowUnsortedKeys", "AllowDuplicateKeys", "AllowMultipleRootValues",
    ];

    /// <summary>Orders byte sequences bytewise, as BEP 3 orders dictionary keys.</summary>
    private static readonly Comparer<byte[]> s_bytewise =
        Comparer<byte[]>.Create((left, right) => left.AsSpan().SequenceCompareTo(right));

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(BencodeReleaseNoteCorpusTests).Assembly));

    /// <summary>
    /// Gets the runnable rows of kind <c>parse</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> ParseRows =>
        RunnableRows("parse");

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RejectRows =>
        RunnableRows("reject");

    /// <summary>
    /// Gets the runnable rows of kind <c>write</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> WriteRows =>
        RunnableRows("write");

    /// <summary>
    /// Gets the runnable rows of kind <c>write-reject</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> WriteRejectRows =>
        RunnableRows("write-reject");

    /// <summary>
    /// Gets the runnable rows of kind <c>roundtrip</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RoundTripRows =>
        RunnableRows("roundtrip");

    /// <summary>
    /// Gets every row of every embedded catalogue.
    /// </summary>
    /// <value>The rows, catalogue by catalogue in file order.</value>
    private static IEnumerable<ReleaseNoteFix> AllRows =>
        s_catalogs.Value.SelectMany(catalog => catalog.Rows);

    /// <summary>
    /// Selects the runnable rows of one kind as test data.
    /// </summary>
    /// <param name="kind">The row kind.</param>
    /// <returns>One single-element argument array per <c>applies</c> or <c>dialect</c> row of <paramref name="kind" />.</returns>
    private static IEnumerable<object[]> RunnableRows(string kind) =>
        AllRows.Where(fix => fix.IsRunnable && fix.Kind == kind).Select(fix => new object[] { fix });

    /// <summary>
    /// Renders the token transcript of an input read through the surface the options select.
    /// </summary>
    /// <param name="input">The bencoded bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The transcript.</returns>
    /// <exception cref="BencodeFormatException">The surface rejects the input.</exception>
    private static string Render(byte[] input, RowOptions options) =>
        options.Surface switch
        {
            "Document" => RenderDocument(input, options),
            "Node" => RenderNode(input, options),
            _ => RenderReader(input, options),
        };

    /// <summary>
    /// Renders the tokens <see cref="Utf8BencodeReader" /> reports until <see cref="Utf8BencodeReader.Read" /> returns
    /// <see langword="false" />.
    /// </summary>
    /// <param name="input">The bencoded bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The transcript.</returns>
    /// <remarks>
    /// An integer is rendered from the input between its <c>i</c> and <c>e</c>, from
    /// <see cref="Utf8BencodeReader.TokenStartIndex" /> to <see cref="Utf8BencodeReader.BytesConsumed" />.
    /// </remarks>
    private static string RenderReader(byte[] input, RowOptions options)
    {
        var reader = new Utf8BencodeReader(input, options.ToReaderOptions());
        var tokens = new List<string>();
        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                BencodeTokenType.StartList => "[",
                BencodeTokenType.EndList => "]",
                BencodeTokenType.StartDictionary => "{",
                BencodeTokenType.EndDictionary => "}",
                BencodeTokenType.PropertyName => "k:" + EncodeToken(reader.ValueSpan),
                BencodeTokenType.ByteString => "s:" + EncodeToken(reader.ValueSpan),
                BencodeTokenType.Integer => "i:" + EncodeToken(input.AsSpan(reader.TokenStartIndex + 1, reader.BytesConsumed - reader.TokenStartIndex - 2)),
                _ => throw new InvalidOperationException($"The reader reported the unexpected token {reader.TokenType}."),
            });
        }

        return string.Join(' ', tokens);
    }

    /// <summary>
    /// Renders a <see cref="BencodeDocument" /> walked from its root element.
    /// </summary>
    /// <param name="input">The bencoded bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The transcript.</returns>
    private static string RenderDocument(byte[] input, RowOptions options)
    {
        using BencodeDocument document = BencodeDocument.Parse(input, options.ToDocumentOptions());
        var tokens = new List<string>();
        WalkElement(document.RootElement, tokens);
        return string.Join(' ', tokens);
    }

    /// <summary>
    /// Renders a <see cref="BencodeNode" /> tree parsed from the input.
    /// </summary>
    /// <param name="input">The bencoded bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The transcript.</returns>
    private static string RenderNode(byte[] input, RowOptions options)
    {
        BencodeNode? root = BencodeNode.Parse(input, default, options.ToDocumentOptions());
        var tokens = new List<string>();
        WalkNode(root, tokens);
        return string.Join(' ', tokens);
    }

    /// <summary>
    /// Appends the tokens of a document element and its descendants.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="tokens">The transcript tokens.</param>
    /// <remarks>
    /// A key is rendered from <see cref="BencodeProperty.Name" /> encoded as UTF-8, and an integer from
    /// <see cref="BencodeElement.GetRawBytes" /> without its <c>i</c> and <c>e</c>.
    /// </remarks>
    private static void WalkElement(BencodeElement element, List<string> tokens)
    {
        switch (element.ValueKind)
        {
            case BencodeValueKind.Object:
                tokens.Add("{");
                foreach (BencodeProperty property in element.EnumerateObject())
                {
                    tokens.Add("k:" + EncodeToken(Encoding.UTF8.GetBytes(property.Name)));
                    WalkElement(property.Value, tokens);
                }

                tokens.Add("}");
                break;

            case BencodeValueKind.Array:
                tokens.Add("[");
                foreach (BencodeElement item in element.EnumerateArray())
                    WalkElement(item, tokens);

                tokens.Add("]");
                break;

            case BencodeValueKind.ByteString:
                tokens.Add("s:" + EncodeToken(element.GetBytes()));
                break;

            case BencodeValueKind.Integer:
                byte[] raw = element.GetRawBytes();
                tokens.Add("i:" + EncodeToken(raw.AsSpan(1, raw.Length - 2)));
                break;

            default:
                throw new InvalidOperationException($"The document reported the unexpected kind {element.ValueKind}.");
        }
    }

    /// <summary>
    /// Appends the tokens of a node and its descendants.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="tokens">The transcript tokens.</param>
    /// <remarks>
    /// A dictionary's entries are rendered in ascending bytewise order of their UTF-8 keys, the order
    /// <see cref="BencodeObject.WriteTo" /> writes them in, because the node model keeps no document order.
    /// </remarks>
    private static void WalkNode(BencodeNode? node, List<string> tokens)
    {
        switch (node)
        {
            case BencodeObject obj:
                tokens.Add("{");
                foreach ((byte[] key, BencodeNode? value) in obj
                    .Select(pair => (Encoding.UTF8.GetBytes(pair.Key), pair.Value))
                    .OrderBy(pair => pair.Item1, s_bytewise))
                {
                    tokens.Add("k:" + EncodeToken(key));
                    WalkNode(value, tokens);
                }

                tokens.Add("}");
                break;

            case BencodeArray array:
                tokens.Add("[");
                foreach (BencodeNode? item in array)
                    WalkNode(item, tokens);

                tokens.Add("]");
                break;

            case BencodeValue value when value.GetValueKind() == BencodeValueKind.ByteString:
                tokens.Add("s:" + EncodeToken(value.GetValue<byte[]>()));
                break;

            case BencodeValue value when value.GetValueKind() == BencodeValueKind.Integer:
                string digits = value.TryGetValue(out long signed)
                    ? signed.ToString(CultureInfo.InvariantCulture)
                    : value.GetValue<ulong>().ToString(CultureInfo.InvariantCulture);
                tokens.Add("i:" + digits);
                break;

            default:
                throw new InvalidOperationException("The node tree holds a null or unexpected node.");
        }
    }

    /// <summary>
    /// Replays a write-notation transcript as calls on a <see cref="Utf8BencodeWriter" />.
    /// </summary>
    /// <param name="operations">The writer calls.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The bytes written and the writer's depth after the last call.</returns>
    /// <remarks>
    /// An integer's digits are written with <c>WriteInteger(long)</c>, or <c>WriteInteger(ulong)</c> above
    /// <see cref="long.MaxValue" />; digits outside both ranges have no typed overload and are written with
    /// <see cref="Utf8BencodeWriter.WriteRawValue(ReadOnlySpan{byte}, bool)" />, validation on.
    /// </remarks>
    private static (byte[] Written, int Depth) Write(IReadOnlyList<WriteOperation> operations, RowOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer, options.ToWriterOptions());
        foreach (WriteOperation operation in operations)
        {
            switch (operation.Token)
            {
                case "[":
                    writer.WriteStartList();
                    break;

                case "]":
                    writer.WriteEndList();
                    break;

                case "{":
                    writer.WriteStartDictionary();
                    break;

                case "}":
                    writer.WriteEndDictionary();
                    break;

                case "k":
                    writer.WritePropertyName(operation.Bytes);
                    break;

                case "s":
                    writer.WriteByteString(operation.Bytes);
                    break;

                default:
                    string digits = Encoding.ASCII.GetString(operation.Bytes);
                    if (long.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long signed))
                        writer.WriteInteger(signed);
                    else if (ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out ulong unsigned))
                        writer.WriteInteger(unsigned);
                    else
                        writer.WriteRawValue(Encoding.ASCII.GetBytes("i" + digits + "e"), skipInputValidation: false);

                    break;
            }
        }

        return (buffer.WrittenSpan.ToArray(), writer.CurrentDepth);
    }

    /// <summary>
    /// Copies the input through <see cref="Utf8BencodeReader" /> into <see cref="Utf8BencodeWriter" /> token by token.
    /// </summary>
    /// <param name="input">The bencoded bytes.</param>
    /// <param name="options">The row options, applied to the reader and the writer.</param>
    /// <returns>The bytes the writer emitted.</returns>
    private static byte[] CopyTokens(byte[] input, RowOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer, options.ToWriterOptions());
        var reader = new Utf8BencodeReader(input, options.ToReaderOptions());
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

                case BencodeTokenType.ByteString:
                    writer.WriteByteString(reader.ValueSpan);
                    break;

                default:
                    if (reader.TryGetInt64(out long signed))
                        writer.WriteInteger(signed);
                    else
                        writer.WriteInteger(reader.GetUInt64());

                    break;
            }
        }

        Assert.AreEqual(0, writer.CurrentDepth, "The token copy left a container open.");
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Reads a write-notation transcript as writer calls.
    /// </summary>
    /// <param name="transcript">The escaped transcript.</param>
    /// <returns>The writer calls, in order.</returns>
    /// <exception cref="FormatException">A token is malformed, or an integer is not canonical base-ten digits.</exception>
    private static List<WriteOperation> ReadWriteNotation(string transcript)
    {
        var operations = new List<WriteOperation>();
        foreach (string token in SplitTranscript(transcript))
        {
            if (token is "[" or "]" or "{" or "}")
            {
                operations.Add(new WriteOperation(token, []));
                continue;
            }

            if (token.Length < 2 || token[1] != ':' || token[0] is not ('k' or 's' or 'i'))
                throw new FormatException($"The write token '{token}' is not [, ], {{, }}, k:, s: or i:.");

            byte[] bytes = CorpusEscapes.Decode(token[2..]);
            if (token[0] == 'i' && !IsCanonicalInteger(bytes))
                throw new FormatException($"The integer token '{token}' is not canonical base-ten digits.");

            operations.Add(new WriteOperation(token[..1], bytes));
        }

        return operations;
    }

    /// <summary>
    /// Brings a transcript to its canonical form: each token's bytes decoded and encoded again.
    /// </summary>
    /// <param name="transcript">The escaped transcript.</param>
    /// <returns>The canonical transcript.</returns>
    /// <exception cref="FormatException">A token is malformed.</exception>
    private static string NormalizeTranscript(string transcript) =>
        string.Join(' ', SplitTranscript(transcript).Select(token => token switch
        {
            "[" or "]" or "{" or "}" => token,
            _ when token.Length >= 2 && token[1] == ':' && token[0] is 'k' or 's' or 'i' =>
                token[..2] + EncodeToken(CorpusEscapes.Decode(token[2..])),
            _ => throw new FormatException($"The transcript token '{token}' is not [, ], {{, }}, k:, s: or i:."),
        }));

    /// <summary>
    /// Splits a transcript into its tokens.
    /// </summary>
    /// <param name="transcript">The transcript.</param>
    /// <returns>The tokens; none for the empty transcript.</returns>
    private static string[] SplitTranscript(string transcript) =>
        transcript.Length == 0 ? [] : transcript.Split(' ');

    /// <summary>
    /// Writes a token's bytes in the catalogue escapes, with a space written <c>\x20</c> so that a transcript splits on
    /// spaces.
    /// </summary>
    /// <param name="bytes">The token's bytes.</param>
    /// <returns>The escaped text.</returns>
    private static string EncodeToken(ReadOnlySpan<byte> bytes) =>
        CorpusEscapes.Encode(bytes).Replace(" ", @"\x20", StringComparison.Ordinal);

    /// <summary>
    /// Determines whether bytes are canonical base-ten integer digits: <c>0</c>, or a nonzero digit after an optional
    /// minus sign, followed by digits.
    /// </summary>
    /// <param name="bytes">The candidate digits.</param>
    /// <returns><see langword="true" /> when the digits are canonical.</returns>
    private static bool IsCanonicalInteger(ReadOnlySpan<byte> bytes)
    {
        if (bytes.SequenceEqual("0"u8))
            return true;

        ReadOnlySpan<byte> digits = bytes.Length > 0 && bytes[0] == (byte)'-' ? bytes[1..] : bytes;
        return digits.Length > 0 && digits[0] is >= (byte)'1' and <= (byte)'9' && !digits.ContainsAnyExceptInRange((byte)'0', (byte)'9');
    }

    /// <summary>
    /// Describes an exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Its type name, its offset when it carries one, and its message.</returns>
    private static string Describe(Exception exception)
    {
        string offset = exception is BencodeFormatException { Offset: int at }
            ? " at offset " + at.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        return $"{exception.GetType().Name}{offset}: {exception.Message}";
    }
}
