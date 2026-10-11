// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Bodu.Test.Corpus;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Holds Bodu.Text.Yaml to the defect fixes other YAML libraries list in their release notes, as catalogued in
/// <c>corpus/yaml/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every input is read with <see cref="YamlDocument.ParseAllDocuments(ReadOnlySpan{byte}, YamlDocumentOptions)" />, so
/// single- and multi-document inputs take one path. A <c>parse</c> row expects the yaml-test-suite <c>in.json</c>
/// convention, one JSON value per document, which <see cref="CorpusCompare.Matches(JsonElement, YamlElement, bool)" />
/// compares with each document, telling integers from floats; the non-finite floats, which JSON cannot hold, are
/// written as the bare tokens <c>.inf</c>, <c>-.inf</c> and <c>.nan</c>.
/// </para>
/// <para>
/// A <c>write</c> row writes the JSON value its input holds through <see cref="Utf8YamlWriter" />, and a
/// <c>roundtrip</c> row writes each document it reads back through <see cref="YamlElement.WriteTo" />, joining the
/// documents of a stream with a <c>---</c> line; both write with the default <see cref="YamlWriterOptions" />. A
/// failure message renders what the library read in the notation the rows use, so it compares directly with the row.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class YamlReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "yaml";

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames = ["SpecVersion", "DuplicateKeyBehavior", "MaxDepth"];

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(YamlReleaseNoteCorpusTests).Assembly));

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
    /// Reads every document of an input with the row's options.
    /// </summary>
    /// <param name="input">The UTF-8 YAML stream.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The documents, in stream order; the caller disposes them.</returns>
    /// <exception cref="YamlFormatException">The library rejects the input.</exception>
    private static IReadOnlyList<YamlDocument> ReadDocuments(byte[] input, RowOptions options) =>
        YamlDocument.ParseAllDocuments(input, options.ToDocumentOptions());

    /// <summary>
    /// Reads every document of an input and renders them.
    /// </summary>
    /// <param name="input">The UTF-8 YAML stream.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering of the documents, as <see cref="Render" /> writes it.</returns>
    /// <exception cref="YamlFormatException">The library rejects the input.</exception>
    private static string ReadAndRender(byte[] input, RowOptions options)
    {
        IReadOnlyList<YamlDocument> documents = ReadDocuments(input, options);
        try
        {
            return Render(documents);
        }
        finally
        {
            DisposeAll(documents);
        }
    }

    /// <summary>
    /// Disposes every document of a stream.
    /// </summary>
    /// <param name="documents">The documents.</param>
    private static void DisposeAll(IReadOnlyList<YamlDocument> documents)
    {
        foreach (YamlDocument document in documents)
            document.Dispose();
    }

    /// <summary>
    /// Reads the expected documents of a <c>parse</c> row.
    /// </summary>
    /// <param name="expected">The row's escaped <c>expected</c> field.</param>
    /// <returns>One JSON value per expected document, in order; none for an empty field.</returns>
    /// <exception cref="FormatException">The field holds an escape that is not valid.</exception>
    /// <exception cref="JsonException">The field does not hold JSON values in the catalogue's notation.</exception>
    private static List<JsonElement> ReadExpectedDocuments(string expected) =>
        CorpusCompare.SplitJsonValues(CorpusCompare.MarkNonFiniteFloats(CorpusEscapes.Decode(expected)));

    /// <summary>
    /// Renders documents in the notation a <c>parse</c> row's <c>expected</c> field uses: one compact JSON value per
    /// document, separated by line feeds, written with the catalogue escapes.
    /// </summary>
    /// <param name="documents">The documents.</param>
    /// <returns>The rendering.</returns>
    /// <remarks>
    /// Mappings keep their stored order. A float renders in round-trip form, with <c>.0</c> added to an integral one,
    /// and the non-finite floats render as the bare tokens <c>.inf</c>, <c>-.inf</c> and <c>.nan</c>. A string escapes
    /// every character outside printable ASCII, so the JSON is ASCII and its escapes survive the catalogue escapes.
    /// </remarks>
    private static string Render(IReadOnlyList<YamlDocument> documents)
    {
        var json = new StringBuilder();
        for (int i = 0; i < documents.Count; i++)
        {
            if (i > 0)
                json.Append('\n');

            AppendElement(json, documents[i].RootElement);
        }

        return CorpusEscapes.Encode(Encoding.ASCII.GetBytes(json.ToString()));
    }

    /// <summary>
    /// Appends the compact JSON rendering of an element and its descendants.
    /// </summary>
    /// <param name="json">The rendering.</param>
    /// <param name="element">The element.</param>
    /// <exception cref="InvalidOperationException">The element has no value kind.</exception>
    private static void AppendElement(StringBuilder json, YamlElement element)
    {
        switch (element.ValueKind)
        {
            case YamlValueKind.Mapping:
                json.Append('{');
                bool firstProperty = true;
                foreach (YamlProperty property in element.EnumerateMapping())
                {
                    if (!firstProperty)
                        json.Append(',');

                    firstProperty = false;
                    AppendString(json, property.Name);
                    json.Append(':');
                    AppendElement(json, property.Value);
                }

                json.Append('}');
                break;

            case YamlValueKind.Sequence:
                json.Append('[');
                bool firstItem = true;
                foreach (YamlElement item in element.EnumerateSequence())
                {
                    if (!firstItem)
                        json.Append(',');

                    firstItem = false;
                    AppendElement(json, item);
                }

                json.Append(']');
                break;

            case YamlValueKind.String:
                AppendString(json, element.GetString());
                break;

            case YamlValueKind.Integer:
                json.Append(element.GetInt64().ToString(CultureInfo.InvariantCulture));
                break;

            case YamlValueKind.Float:
                json.Append(FormatFloat(element.GetDouble()));
                break;

            case YamlValueKind.Boolean:
                json.Append(element.GetBoolean() ? "true" : "false");
                break;

            case YamlValueKind.Null:
                json.Append("null");
                break;

            default:
                throw new InvalidOperationException($"The document holds an element of the unexpected kind {element.ValueKind}.");
        }
    }

    /// <summary>
    /// Formats a float for a rendering.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// <c>.nan</c>, <c>.inf</c> or <c>-.inf</c> for a non-finite value; otherwise the round-trip text, with <c>.0</c>
    /// added when it has neither a fraction nor an exponent.
    /// </returns>
    private static string FormatFloat(double value)
    {
        if (double.IsNaN(value))
            return ".nan";

        if (double.IsInfinity(value))
            return value > 0 ? ".inf" : "-.inf";

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.AsSpan().IndexOfAny('.', 'E', 'e') < 0 ? text + ".0" : text;
    }

    /// <summary>
    /// Appends a string as a JSON string literal that holds only printable ASCII.
    /// </summary>
    /// <param name="json">The rendering.</param>
    /// <param name="value">The string.</param>
    /// <remarks>
    /// A quotation mark and a backslash are escaped, a line feed, a carriage return and a tab use their short escapes,
    /// and every other UTF-16 code unit outside printable ASCII is written as a four-digit lowercase escape.
    /// </remarks>
    private static void AppendString(StringBuilder json, string value)
    {
        json.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    json.Append('\\').Append('"');
                    break;

                case '\\':
                    json.Append('\\').Append('\\');
                    break;

                case '\n':
                    json.Append('\\').Append('n');
                    break;

                case '\r':
                    json.Append('\\').Append('r');
                    break;

                case '\t':
                    json.Append('\\').Append('t');
                    break;

                default:
                    if (c is < ' ' or > '~')
                        json.Append('\\').Append('u').Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        json.Append(c);

                    break;
            }
        }

        json.Append('"');
    }

    /// <summary>
    /// Writes the JSON value of a <c>write</c> row through <see cref="Utf8YamlWriter" /> with the default options.
    /// </summary>
    /// <param name="utf8Json">The JSON value.</param>
    /// <returns>The YAML the writer emitted.</returns>
    /// <exception cref="JsonException"><paramref name="utf8Json" /> is not a single JSON value.</exception>
    private static byte[] WriteJsonValue(byte[] utf8Json)
    {
        using var json = JsonDocument.Parse(utf8Json);
        var output = new ArrayBufferWriter<byte>();
        WriteValue(json.RootElement, new Utf8YamlWriter(output));
        return output.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Writes a JSON value and its descendants through a writer.
    /// </summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="writer">The writer; a copy shares the state of the writer it was copied from.</param>
    /// <remarks>
    /// An object is written as a mapping in property order and an array as a sequence. A number that reads as a 64-bit
    /// integer, which needs a literal with neither a fraction nor an exponent, is written with
    /// <see cref="Utf8YamlWriter.WriteInteger" />, and any other number with <see cref="Utf8YamlWriter.WriteDouble" />.
    /// </remarks>
    private static void WriteValue(JsonElement value, Utf8YamlWriter writer)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartMapping();
                foreach (JsonProperty property in value.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteValue(property.Value, writer);
                }

                writer.WriteEndMapping();
                break;

            case JsonValueKind.Array:
                writer.WriteStartSequence();
                foreach (JsonElement item in value.EnumerateArray())
                    WriteValue(item, writer);

                writer.WriteEndSequence();
                break;

            case JsonValueKind.String:
                writer.WriteString(value.GetString()!);
                break;

            case JsonValueKind.Number:
                if (value.TryGetInt64(out long integer))
                    writer.WriteInteger(integer);
                else
                    writer.WriteDouble(value.GetDouble());

                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                writer.WriteBoolean(value.GetBoolean());
                break;

            default:
                writer.WriteNull();
                break;
        }
    }

    /// <summary>
    /// Writes documents back as one stream, each through its own <see cref="Utf8YamlWriter" /> with the default options.
    /// </summary>
    /// <param name="documents">The documents.</param>
    /// <returns>The written stream, a <c>---</c> line between consecutive documents.</returns>
    private static byte[] WriteDocuments(IReadOnlyList<YamlDocument> documents)
    {
        var output = new ArrayBufferWriter<byte>();
        for (int i = 0; i < documents.Count; i++)
        {
            if (i > 0)
                output.Write("---\n"u8);

            documents[i].RootElement.WriteTo(new Utf8YamlWriter(output));
        }

        return output.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Describes an exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Its type name, the position it reports when it is a <see cref="YamlFormatException" />, and its message.</returns>
    private static string Describe(Exception exception) =>
        exception is YamlFormatException { LineNumber: int line, ColumnNumber: int column }
            ? string.Create(CultureInfo.InvariantCulture, $"{exception.GetType().Name} at line {line}, column {column}: {exception.Message}")
            : $"{exception.GetType().Name}: {exception.Message}";
}
