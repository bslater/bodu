// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Bodu.Test.Corpus;
using Bodu.Text.Delimited.Reader;
using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited;

/// <summary>
/// Holds Bodu.Text.Delimited to the defect fixes other CSV libraries list in their release notes, as catalogued in
/// <c>corpus/delimited/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A read is rendered as compact JSON: an array of records, each as <see cref="Utf8DelimitedReader" /> frames it. In
/// header mode a record is an array of <c>[name, value]</c> pairs in field order, the names being the
/// <see cref="DelimitedTokenType.PropertyName" /> tokens and the values the <see cref="DelimitedTokenType.String" />
/// tokens, so duplicate, empty and synthesized names stay visible; with <c>NoHeader=true</c> a record is an array of
/// values. With <c>View=Headers</c> the rendering is the reader's <see cref="Utf8DelimitedReader.Headers" /> list once
/// the whole input is read, as an array of strings. JSON strings escape only <c>"</c>, <c>\</c> and the C0 controls;
/// all other text, non-ASCII included, is itself.
/// </para>
/// <para>
/// A write row's input is the same JSON shape, replayed on <see cref="Utf8DelimitedWriter" />: in header mode each
/// record as an object record, whose first record supplies the header row, and with <c>NoHeader=true</c> each record as
/// a positional record. A JSON <c>null</c> name or value reaches the writer as <see langword="null" />. A round-trip
/// row reads its input, writes the records it read back with the row's dialect, and reads the written bytes again.
/// </para>
/// <para>
/// Expected and actual results are compared in the catalogue's escape notation, each well-formed UTF-8 sequence written
/// <c>\u{H...}</c>, so that a failure shows both sides as a catalogue row writes them.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "delimited";

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames =
    [
        "Delimiter", "Quote", "CommentChar", "NoHeader", "TrimFields", "AllowComments",
        "FieldCountBehavior", "MalformedRecordBehavior", "DuplicateHeaderBehavior", "View",
    ];

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(DelimitedReleaseNoteCorpusTests).Assembly));

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
    /// <returns>
    /// One single-element argument array per <c>applies</c> or <c>dialect</c> row of <paramref name="kind" />.
    /// </returns>
    private static IEnumerable<object[]> RunnableRows(string kind) =>
        AllRows.Where(fix => fix.IsRunnable && fix.Kind == kind).Select(fix => new object[] { fix });

    /// <summary>
    /// Reads a whole document with <see cref="Utf8DelimitedReader" />, recording each record as the token stream frames
    /// it.
    /// </summary>
    /// <param name="input">The UTF-8 input.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The records, and the header names the reader reports once the input is read.</returns>
    /// <exception cref="DelimitedFormatException">The reader rejects the input.</exception>
    /// <exception cref="InvalidOperationException">The token stream is not an array of records.</exception>
    private static FramedDocument ReadDocument(ReadOnlySpan<byte> input, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(input, options);
        var records = new List<FramedRecord>();
        FramedRecord? record = null;
        string? pendingName = null;
        int depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray when depth == 0:
                    depth = 1;
                    break;

                case DelimitedTokenType.StartArray when depth == 1:
                    record = new FramedRecord(IsObject: false, []);
                    depth = 2;
                    break;

                case DelimitedTokenType.StartObject when depth == 1:
                    record = new FramedRecord(IsObject: true, []);
                    depth = 2;
                    break;

                case DelimitedTokenType.PropertyName when depth == 2 && record is { IsObject: true } && pendingName is null:
                    pendingName = reader.GetString();
                    break;

                case DelimitedTokenType.String when depth == 2 && record is not null && record.IsObject == (pendingName is not null):
                    record.Fields.Add((pendingName, reader.GetString()));
                    pendingName = null;
                    break;

                case DelimitedTokenType.EndArray when depth == 2 && record is { IsObject: false }:
                case DelimitedTokenType.EndObject when depth == 2 && record is { IsObject: true } && pendingName is null:
                    records.Add(record);
                    record = null;
                    depth = 1;
                    break;

                case DelimitedTokenType.EndArray when depth == 1:
                    depth = 0;
                    break;

                default:
                    throw new InvalidOperationException($"The reader reported the unexpected token {reader.TokenType} at depth {depth}.");
            }
        }

        if (depth != 0)
            throw new InvalidOperationException("The token stream ended inside the document.");

        return new FramedDocument([.. reader.Headers], records);
    }

    /// <summary>
    /// Writes records with <see cref="Utf8DelimitedWriter" />: an object record as
    /// <see cref="Utf8DelimitedWriter.WriteStartObject" />, a
    /// <see cref="Utf8DelimitedWriter.WritePropertyName(string)" /> and
    /// <see cref="Utf8DelimitedWriter.WriteString(string)" /> call per field, and
    /// <see cref="Utf8DelimitedWriter.WriteEndObject" />, and a positional record as a nested
    /// <see cref="Utf8DelimitedWriter.WriteStartArray" /> of <see cref="Utf8DelimitedWriter.WriteString(string)" />
    /// calls.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <param name="options">The writer options.</param>
    /// <returns>The written bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// A name or value is <see langword="null" />, which the writer rejects.
    /// </exception>
    private static byte[] WriteRecords(IReadOnlyList<FramedRecord> records, DelimitedWriterOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer, options);

        writer.WriteStartArray();
        foreach (FramedRecord record in records)
        {
            // A JSON null in the write notation reaches the writer as null, which a write-reject row expects it to
            // refuse.
            if (record.IsObject)
            {
                writer.WriteStartObject();
                foreach ((string? name, string? value) in record.Fields)
                {
                    writer.WritePropertyName(name!);
                    writer.WriteString(value!);
                }

                writer.WriteEndObject();
            }
            else
            {
                writer.WriteStartArray();
                foreach ((_, string? value) in record.Fields)
                    writer.WriteString(value!);

                writer.WriteEndArray();
            }
        }

        writer.WriteEndArray();
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Reads a write row's input, which uses the JSON shape of the rendering: an array of records, each an array of
    /// <c>[name, value]</c> pairs in header mode, or an array of values with <c>NoHeader=true</c>.
    /// </summary>
    /// <param name="input">The decoded input bytes.</param>
    /// <param name="noHeader">Whether the records are positional.</param>
    /// <returns>The records, each JSON <c>null</c> name or value kept as <see langword="null" />.</returns>
    /// <exception cref="FormatException">The input is not that JSON shape.</exception>
    private static List<FramedRecord> ReadWriteNotation(byte[] input, bool noHeader)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(input);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"The write input is not JSON: {ex.Message}", ex);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                throw new FormatException("The write input is not a JSON array of records.");

            var records = new List<FramedRecord>();
            foreach (JsonElement recordElement in root.EnumerateArray())
            {
                if (recordElement.ValueKind != JsonValueKind.Array)
                    throw new FormatException("A record of the write input is not a JSON array.");

                var record = new FramedRecord(!noHeader, []);
                foreach (JsonElement field in recordElement.EnumerateArray())
                {
                    if (noHeader)
                    {
                        record.Fields.Add((null, StringOrNull(field)));
                        continue;
                    }

                    if (field.ValueKind != JsonValueKind.Array || field.GetArrayLength() != 2)
                        throw new FormatException("A field of a header-mode write input is not a [name, value] pair.");

                    record.Fields.Add((StringOrNull(field[0]), StringOrNull(field[1])));
                }

                records.Add(record);
            }

            return records;
        }
    }

    /// <summary>
    /// Reads a JSON string or <c>null</c> of the write notation.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The string, or <see langword="null" /> for a JSON <c>null</c>.</returns>
    /// <exception cref="FormatException">The element is neither a string nor <c>null</c>.</exception>
    private static string? StringOrNull(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            _ => throw new FormatException($"A name or value of the write input is a JSON {element.ValueKind}, not a string or null."),
        };

    /// <summary>
    /// Renders a document as compact JSON: the records as an array of records (an object record as an array of
    /// <c>[name, value]</c> pairs, a positional record as an array of values), or the header names.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="view">What to render.</param>
    /// <returns>The rendering.</returns>
    private static string Render(FramedDocument document, RowView view)
    {
        var text = new StringBuilder();
        text.Append('[');
        if (view == RowView.Headers)
        {
            for (int i = 0; i < document.Headers.Count; i++)
            {
                if (i > 0)
                    text.Append(',');

                AppendJsonString(text, document.Headers[i]);
            }
        }
        else
        {
            for (int r = 0; r < document.Records.Count; r++)
            {
                if (r > 0)
                    text.Append(',');

                FramedRecord record = document.Records[r];
                text.Append('[');
                for (int f = 0; f < record.Fields.Count; f++)
                {
                    if (f > 0)
                        text.Append(',');

                    (string? name, string? value) = record.Fields[f];
                    if (record.IsObject)
                    {
                        text.Append('[');
                        AppendJsonString(text, name);
                        text.Append(',');
                        AppendJsonString(text, value);
                        text.Append(']');
                    }
                    else
                    {
                        AppendJsonString(text, value);
                    }
                }

                text.Append(']');
            }
        }

        return text.Append(']').ToString();
    }

    /// <summary>
    /// Appends a JSON string, escaping only what JSON requires: <c>"</c>, <c>\</c> and the C0 controls, by their short
    /// escapes where JSON has one and as <c>\u00XX</c> with upper-case hexadecimal digits otherwise.
    /// </summary>
    /// <param name="text">The destination.</param>
    /// <param name="value">The string, or <see langword="null" /> to append the JSON <c>null</c>.</param>
    private static void AppendJsonString(StringBuilder text, string? value)
    {
        if (value is null)
        {
            text.Append("null");
            return;
        }

        text.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    text.Append("\\\"");
                    break;

                case '\\':
                    text.Append(@"\\");
                    break;

                case '\b':
                    text.Append(@"\b");
                    break;

                case '\f':
                    text.Append(@"\f");
                    break;

                case '\n':
                    text.Append(@"\n");
                    break;

                case '\r':
                    text.Append(@"\r");
                    break;

                case '\t':
                    text.Append(@"\t");
                    break;

                default:
                    if (c < ' ')
                        text.Append(@"\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        text.Append(c);

                    break;
            }
        }

        text.Append('"');
    }

    /// <summary>
    /// Writes bytes in the catalogue's escape notation: printable ASCII as itself, a backslash, a line feed, a carriage
    /// return, a tab and a NUL by their short escapes, each well-formed UTF-8 sequence as <c>\u{H...}</c>, and any
    /// other byte as <c>\xHH</c>.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>
    /// The escaped text, which <see cref="CorpusEscapes.Decode" /> turns back into <paramref name="bytes" />.
    /// </returns>
    private static string Escape(ReadOnlySpan<byte> bytes)
    {
        var text = new StringBuilder(bytes.Length);
        int i = 0;
        while (i < bytes.Length)
        {
            byte b = bytes[i];
            switch (b)
            {
                case (byte)'\\':
                    text.Append(@"\\");
                    i++;
                    continue;

                case (byte)'\n':
                    text.Append(@"\n");
                    i++;
                    continue;

                case (byte)'\r':
                    text.Append(@"\r");
                    i++;
                    continue;

                case (byte)'\t':
                    text.Append(@"\t");
                    i++;
                    continue;

                case 0:
                    text.Append(@"\0");
                    i++;
                    continue;
            }

            if (b is >= (byte)' ' and <= (byte)'~')
            {
                text.Append((char)b);
                i++;
                continue;
            }

            if (b >= 0x80 && Rune.DecodeFromUtf8(bytes[i..], out Rune rune, out int consumed) == OperationStatus.Done)
            {
                text.Append(@"\u{").Append(rune.Value.ToString("X", CultureInfo.InvariantCulture)).Append('}');
                i += consumed;
                continue;
            }

            text.Append(@"\x").Append(b.ToString("X2", CultureInfo.InvariantCulture));
            i++;
        }

        return text.ToString();
    }

    /// <summary>
    /// Writes text in the catalogue's escape notation, as <see cref="Escape(ReadOnlySpan{byte})" /> writes its UTF-8
    /// encoding.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The escaped text.</returns>
    private static string Escape(string text) =>
        Escape(Encoding.UTF8.GetBytes(text));

    /// <summary>
    /// Describes an exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>
    /// Its type name, the line and offset a <see cref="DelimitedFormatException" /> carries, and its message.
    /// </returns>
    private static string Describe(Exception exception)
    {
        string position = exception is DelimitedFormatException { LineNumber: int line, Offset: int offset }
            ? string.Create(CultureInfo.InvariantCulture, $" at line {line}, offset {offset}")
            : string.Empty;

        return $"{exception.GetType().Name}{position}: {Escape(exception.Message)}";
    }
}
