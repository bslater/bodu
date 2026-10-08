// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Bodu.Test.Corpus;
using Bodu.Text.DotEnv.Reader;
using Bodu.Text.DotEnv.Writer;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Holds Bodu.Text.DotEnv to the defect fixes other dotenv libraries list in their release notes, as catalogued in
/// <c>corpus/dotenv/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A read is rendered as a compact JSON array of the entries <see cref="Utf8DotEnvReader" /> reports, in source order:
/// <c>["KEY","value"]</c>, or <c>["KEY","value","export"]</c> when the line carried the <c>export</c> prefix. Comments
/// are not rendered. A string escapes <c>"</c> and <c>\</c> with a backslash, writes U+0008, U+0009, U+000A, U+000C
/// and U+000D as <c>\b</c>, <c>\t</c>, <c>\n</c>, <c>\f</c> and <c>\r</c>, every other character below U+0020 as
/// <c>\u</c> followed by four upper-case hexadecimal digits, and everything else as itself.
/// </para>
/// <para>
/// The write notation is the same JSON shape, replayed on <see cref="Utf8DotEnvWriter" /> between
/// <see cref="Utf8DotEnvWriter.WriteStartObject" /> and <see cref="Utf8DotEnvWriter.WriteEndObject" />: an entry that
/// carries <c>"export"</c> is named with <see cref="Utf8DotEnvWriter.WritePropertyName(string, bool)" />, any other
/// with <see cref="Utf8DotEnvWriter.WritePropertyName(string)" />, which applies
/// <see cref="DotEnvWriterOptions.WriteExportPrefix" />, and every value is written with
/// <see cref="Utf8DotEnvWriter.WriteString(string)" />. A round-trip row writes the entries it reads in the same way,
/// then reads what was written.
/// </para>
/// <para>
/// Renderings and written bytes are compared in the catalogue escapes, each side decoded to bytes and encoded again by
/// <see cref="CorpusEscapes" />, so the comparison is byte for byte and a failure prints only printable ASCII.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class DotEnvReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "dotenv";

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames =
    [
        "DisallowExportPrefix", "DisallowInlineComments", "SkipComments", "WriteExportPrefix",
    ];

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(DotEnvReleaseNoteCorpusTests).Assembly));

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
    /// Reads every entry of a document with <see cref="Utf8DotEnvReader" />, in source order.
    /// </summary>
    /// <param name="input">The document bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The entries; comments are not included.</returns>
    /// <exception cref="DotEnvFormatException">The reader rejects the document.</exception>
    private static List<Entry> ReadEntries(ReadOnlySpan<byte> input, RowOptions options)
    {
        var reader = new Utf8DotEnvReader(input, options.ToReaderOptions());
        var entries = new List<Entry>();
        string? key = null;
        bool export = false;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DotEnvTokenType.PropertyName:
                    key = reader.GetString();
                    export = reader.CurrentIsExport;
                    break;

                case DotEnvTokenType.String:
                    Assert.IsNotNull(key, "The reader reported a value before its key.");
                    entries.Add(new Entry(key, reader.GetString(), export));
                    key = null;
                    break;

                default:
                    break;
            }
        }

        return entries;
    }

    /// <summary>
    /// Renders entries as the compact JSON array a <c>parse</c> row expects.
    /// </summary>
    /// <param name="entries">The entries, in source order.</param>
    /// <returns>The rendering.</returns>
    private static string Render(IReadOnlyList<Entry> entries)
    {
        var builder = new StringBuilder("[");

        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0)
                builder.Append(',');

            builder.Append('[');
            AppendJsonString(builder, entries[i].Key);
            builder.Append(',');
            AppendJsonString(builder, entries[i].Value);
            if (entries[i].Export)
                builder.Append(",\"export\"");

            builder.Append(']');
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// Appends a string as a JSON string literal under the rendering's escaping rules.
    /// </summary>
    /// <param name="builder">The rendering being built.</param>
    /// <param name="value">The string to append.</param>
    private static void AppendJsonString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append(@"\\");
                    break;

                case '\b':
                    builder.Append(@"\b");
                    break;

                case '\t':
                    builder.Append(@"\t");
                    break;

                case '\n':
                    builder.Append(@"\n");
                    break;

                case '\f':
                    builder.Append(@"\f");
                    break;

                case '\r':
                    builder.Append(@"\r");
                    break;

                default:
                    if (c < ' ')
                        builder.Append(CultureInfo.InvariantCulture, $"\\u{(int)c:X4}");
                    else
                        builder.Append(c);

                    break;
            }
        }

        builder.Append('"');
    }

    /// <summary>
    /// Reads a field written in the write notation, the JSON shape of <see cref="Render" />.
    /// </summary>
    /// <param name="field">The escaped field.</param>
    /// <returns>The entries, in order.</returns>
    /// <exception cref="FormatException">
    /// The field does not decode, or is not a JSON array of <c>[key, value]</c> and <c>[key, value, "export"]</c>
    /// arrays of strings.
    /// </exception>
    private static List<Entry> ReadWriteNotation(string field)
    {
        using JsonDocument document = ParseJson(CorpusEscapes.Decode(field));

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("The write notation is not a JSON array of entries.");

        var entries = new List<Entry>();
        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() is < 2 or > 3)
                throw new FormatException("A write-notation entry is not [key, value] or [key, value, \"export\"].");

            JsonElement[] parts = [.. item.EnumerateArray()];
            if (parts.Any(part => part.ValueKind != JsonValueKind.String))
                throw new FormatException("A write-notation entry holds something other than strings.");

            if (parts.Length == 3 && parts[2].GetString() != "export")
                throw new FormatException("The third element of a write-notation entry is not \"export\".");

            entries.Add(new Entry(parts[0].GetString()!, parts[1].GetString()!, parts.Length == 3));
        }

        return entries;
    }

    /// <summary>
    /// Parses the UTF-8 JSON of a write-notation field.
    /// </summary>
    /// <param name="utf8Json">The decoded field.</param>
    /// <returns>The parsed document, which the caller disposes.</returns>
    /// <exception cref="FormatException">The bytes are not JSON.</exception>
    private static JsonDocument ParseJson(byte[] utf8Json)
    {
        try
        {
            return JsonDocument.Parse(utf8Json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"The write notation is not JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes entries with <see cref="Utf8DotEnvWriter" /> as the write notation prescribes.
    /// </summary>
    /// <param name="entries">The entries to write, in order.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The written bytes.</returns>
    private static byte[] Write(IReadOnlyList<Entry> entries, RowOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DotEnvWriter(buffer, options.ToWriterOptions());

        writer.WriteStartObject();
        foreach (Entry entry in entries)
        {
            if (entry.Export)
                writer.WritePropertyName(entry.Key, export: true);
            else
                writer.WritePropertyName(entry.Key);

            writer.WriteString(entry.Value);
        }

        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Brings an escaped field to its canonical form: decoded to bytes and encoded again.
    /// </summary>
    /// <param name="field">The escaped field.</param>
    /// <returns>The canonical escaped text, which compares equal exactly when the bytes do.</returns>
    /// <exception cref="FormatException">The field holds a character or escape that is not valid.</exception>
    private static string Canonicalize(string field) =>
        CorpusEscapes.Encode(CorpusEscapes.Decode(field));

    /// <summary>
    /// Writes text in the canonical escaped form of its UTF-8 bytes.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The escaped text, comparable with <see cref="Canonicalize" />.</returns>
    private static string EncodeText(string text) =>
        CorpusEscapes.Encode(Encoding.UTF8.GetBytes(text));
}
