// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Bodu.Test.Corpus;
using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini;

/// <summary>
/// Holds Bodu.Text.Ini to the defect fixes other INI libraries list in their release notes, as catalogued in
/// <c>corpus/ini/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A read is rendered in the view the <c>View</c> option selects. The <c>Entries</c> view, the default, is a compact
/// JSON array of <c>[section, key, value]</c> triples in source order, read with <see cref="Utf8IniReader" />, with
/// <c>""</c> as the section of the keys before the first header, so <c>a=1\n[s]\nb=2\n</c> renders as
/// <c>[["","a","1"],["s","b","2"]]</c>. The <c>Document</c> view is a compact JSON object of objects read with
/// <see cref="IniDocumentReader" />, after the duplicate policies: the global keys form the object <c>""</c>, present
/// only when the document has global keys and always first, and the sections follow in the order they first appear.
/// </para>
/// <para>
/// A write-reject row's input is the write notation: a JSON array of <c>[section, key, value]</c> string triples,
/// written with <see cref="Utf8IniWriter" />, a section header each time the section changes, the global triples first
/// and without a header. A round-trip row writes a read back with <see cref="Utf8IniWriter" />, every token of the
/// source-order reader in the <c>Entries</c> view and the normalized document in the <c>Document</c> view, and reads
/// the written text again.
/// </para>
/// <para>
/// The catalogue holds no <c>write</c> row, so this class has no data-driven test for that kind: MSTest fails a test
/// whose data source is empty. <see cref="FixCatalogues_WhenRunnableRowsAreRead_ShouldHoldWellFormedScenarios" />
/// reports a <c>write</c> row if one is catalogued, so that its runner is added with it.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class IniReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "ini";

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames =
    [
        "DisallowHashComments", "SkipComments", "DuplicateSectionBehavior", "DuplicateKeyBehavior", "CommentPrefix", "View",
    ];

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(IniReleaseNoteCorpusTests).Assembly));

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
    /// Renders an input in the view the row options select.
    /// </summary>
    /// <param name="input">The INI bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering.</returns>
    /// <exception cref="IniFormatException">The input is not valid INI, or a duplicate policy rejects it.</exception>
    private static string Render(byte[] input, RowOptions options) =>
        options.DocumentView
            ? RenderDocument(input, options)
            : RenderEntries(input, options);

    /// <summary>
    /// Renders the <c>Entries</c> view: the <c>[section, key, value]</c> triple of every entry
    /// <see cref="Utf8IniReader" /> reports, in source order.
    /// </summary>
    /// <param name="input">The INI bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>A compact JSON array of string triples; <c>""</c> is the section of the keys before the first header.</returns>
    /// <exception cref="IniFormatException">The input is not valid INI.</exception>
    /// <remarks>Comment tokens are not part of the rendering.</remarks>
    private static string RenderEntries(byte[] input, RowOptions options)
    {
        var builder = new StringBuilder("[");
        var reader = new Utf8IniReader(input, options.ToReaderOptions());
        string section = string.Empty;
        string key = string.Empty;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case IniTokenType.SectionHeader:
                    section = reader.GetString();
                    break;

                case IniTokenType.PropertyName:
                    key = reader.GetString();
                    break;

                case IniTokenType.String:
                    if (builder.Length > 1)
                        builder.Append(',');

                    builder.Append('[');
                    AppendJsonString(builder, section);
                    builder.Append(',');
                    AppendJsonString(builder, key);
                    builder.Append(',');
                    AppendJsonString(builder, reader.GetString());
                    builder.Append(']');
                    break;

                case IniTokenType.Comment:
                    break;

                default:
                    throw new InvalidOperationException($"The source-order reader reported the unexpected token {reader.TokenType}.");
            }
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// Renders the <c>Document</c> view: the normalized document <see cref="IniDocumentReader" /> walks, after the
    /// duplicate policies.
    /// </summary>
    /// <param name="input">The INI bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>
    /// A compact JSON object of objects: the global keys as the object <c>""</c>, present only when there are global keys
    /// and always first, then each section in first-appearance order.
    /// </returns>
    /// <exception cref="IniFormatException">The input is not valid INI, or a duplicate policy rejects it.</exception>
    private static string RenderDocument(byte[] input, RowOptions options)
    {
        var globals = new StringBuilder();
        var sections = new StringBuilder();
        var reader = new IniDocumentReader(input, options.ToReaderOptions(), options.ToDocumentOptions());
        int depth = 0;
        string name = string.Empty;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case IniTokenType.StartObject:
                    depth++;
                    if (depth == 2)
                    {
                        if (sections.Length > 0)
                            sections.Append(',');

                        AppendJsonString(sections, name);
                        sections.Append(":{");
                    }

                    break;

                case IniTokenType.EndObject:
                    if (depth == 2)
                        sections.Append('}');

                    depth--;
                    break;

                case IniTokenType.PropertyName:
                    name = reader.GetString();
                    break;

                case IniTokenType.String:
                    // A global entry is read at depth one and a section's at depth two; an entry follows another entry
                    // of its scope after a comma, and the opening brace of a section without one.
                    StringBuilder scope = depth == 1 ? globals : sections;
                    if (scope.Length > 0 && scope[^1] != '{')
                        scope.Append(',');

                    AppendJsonString(scope, name);
                    scope.Append(':');
                    AppendJsonString(scope, reader.GetString());
                    break;

                default:
                    throw new InvalidOperationException($"The document reader reported the unexpected token {reader.TokenType}.");
            }
        }

        var builder = new StringBuilder("{");
        if (globals.Length > 0)
        {
            builder.Append("\"\":{").Append(globals).Append('}');
            if (sections.Length > 0)
                builder.Append(',');
        }

        return builder.Append(sections).Append('}').ToString();
    }

    /// <summary>
    /// Reads the write notation: a JSON array of <c>[section, key, value]</c> string triples.
    /// </summary>
    /// <param name="field">The escaped <c>input</c> field of a row.</param>
    /// <returns>The triples, in order.</returns>
    /// <exception cref="FormatException">
    /// The field's escapes are not valid, the notation is not a JSON array of string triples, or a triple of the global
    /// section <c>""</c> follows a triple of a named section, which the writer could not place before the first header.
    /// </exception>
    private static List<(string Section, string Key, string Value)> ReadWriteNotation(string field)
    {
        byte[] json = CorpusEscapes.Decode(field);
        var triples = new List<(string Section, string Key, string Value)>();

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new FormatException("The write notation is not a JSON array.");

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Array
                    || element.GetArrayLength() != 3
                    || element.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
                {
                    throw new FormatException("An element of the write notation is not a [section, key, value] string triple.");
                }

                string section = element[0].GetString()!;
                if (section.Length == 0 && triples.Count > 0 && triples[^1].Section.Length > 0)
                    throw new FormatException("A triple of the global section follows a triple of a named section.");

                triples.Add((section, element[1].GetString()!, element[2].GetString()!));
            }
        }
        catch (JsonException ex)
        {
            throw new FormatException($"The write notation is not valid JSON: {ex.Message}", ex);
        }

        return triples;
    }

    /// <summary>
    /// Writes triples with <see cref="Utf8IniWriter" />: a section header each time the section changes, then one entry
    /// per triple.
    /// </summary>
    /// <param name="triples">The triples, the global ones first.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The bytes written.</returns>
    private static byte[] WriteTriples(IReadOnlyList<(string Section, string Key, string Value)> triples, RowOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer, options.ToWriterOptions());
        string current = string.Empty;

        foreach ((string section, string key, string value) in triples)
        {
            if (!string.Equals(section, current, StringComparison.Ordinal))
            {
                writer.WriteSectionHeader(section);
                current = section;
            }

            writer.WritePropertyName(key);
            writer.WriteString(value);
        }

        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Writes a read back with <see cref="Utf8IniWriter" /> in the view the row options select.
    /// </summary>
    /// <param name="input">The INI bytes.</param>
    /// <param name="options">The row options, applied to the reading and to the writer.</param>
    /// <returns>The bytes written.</returns>
    /// <exception cref="IniFormatException">The input is not valid INI, or a duplicate policy rejects it.</exception>
    private static byte[] WriteBack(byte[] input, RowOptions options) =>
        options.DocumentView
            ? CopyDocument(input, options)
            : CopyTokens(input, options);

    /// <summary>
    /// Copies every token <see cref="Utf8IniReader" /> reports to <see cref="Utf8IniWriter" />, in source order: section
    /// headers, comments with the writer's comment prefix, and entries.
    /// </summary>
    /// <param name="input">The INI bytes.</param>
    /// <param name="options">The row options, applied to the reader and the writer.</param>
    /// <returns>The bytes written.</returns>
    /// <exception cref="IniFormatException">The input is not valid INI.</exception>
    private static byte[] CopyTokens(byte[] input, RowOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer, options.ToWriterOptions());
        var reader = new Utf8IniReader(input, options.ToReaderOptions());

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case IniTokenType.SectionHeader:
                    writer.WriteSectionHeader(reader.GetString());
                    break;

                case IniTokenType.Comment:
                    writer.WriteComment(reader.GetString());
                    break;

                case IniTokenType.PropertyName:
                    writer.WritePropertyName(reader.GetString());
                    break;

                case IniTokenType.String:
                    writer.WriteString(reader.GetString());
                    break;

                default:
                    throw new InvalidOperationException($"The source-order reader reported the unexpected token {reader.TokenType}.");
            }
        }

        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Writes the normalized document <see cref="IniDocumentReader" /> walks with <see cref="Utf8IniWriter" />: the global
    /// entries, then each section's header and entries.
    /// </summary>
    /// <param name="input">The INI bytes.</param>
    /// <param name="options">The row options, applied to the reader and the writer.</param>
    /// <returns>The bytes written.</returns>
    /// <exception cref="IniFormatException">The input is not valid INI, or a duplicate policy rejects it.</exception>
    private static byte[] CopyDocument(byte[] input, RowOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer, options.ToWriterOptions());
        var reader = new IniDocumentReader(input, options.ToReaderOptions(), options.ToDocumentOptions());
        int depth = 0;
        string name = string.Empty;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case IniTokenType.StartObject:
                    depth++;
                    if (depth == 2)
                        writer.WriteSectionHeader(name);

                    break;

                case IniTokenType.EndObject:
                    depth--;
                    break;

                case IniTokenType.PropertyName:
                    name = reader.GetString();
                    break;

                case IniTokenType.String:
                    writer.WritePropertyName(name);
                    writer.WriteString(reader.GetString());
                    break;

                default:
                    throw new InvalidOperationException($"The document reader reported the unexpected token {reader.TokenType}.");
            }
        }

        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Appends a JSON string literal in the form the renderings use.
    /// </summary>
    /// <param name="builder">The destination.</param>
    /// <param name="value">The string.</param>
    /// <remarks>
    /// <c>"</c> and <c>\</c> are escaped; U+0008, U+0009, U+000A, U+000C and U+000D are written <c>\b</c>, <c>\t</c>,
    /// <c>\n</c>, <c>\f</c> and <c>\r</c>; the other control characters and U+007F are written <c>\u00XX</c> with
    /// upper-case hexadecimal digits; and every other character is written as itself.
    /// </remarks>
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
                    if (c < ' ' || c == '\u007F')
                        builder.Append(@"\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(c);

                    break;
            }
        }

        builder.Append('"');
    }
}
