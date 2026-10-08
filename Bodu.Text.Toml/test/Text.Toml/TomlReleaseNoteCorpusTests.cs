// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Bodu.Test.Corpus;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Holds Bodu.Text.Toml to the defect fixes other TOML libraries list in their release notes, as catalogued in
/// <c>corpus/toml/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A read is described in toml-test's tagged JSON: a table is a JSON object, an array a JSON array, and every scalar an
/// object of exactly <c>{"type": ..., "value": ...}</c>. A <c>parse</c> row reads its input with
/// <see cref="TomlDocumentReader" /> into the comparison model of <see cref="TomlTestCorpusTests" /> and compares it with
/// the expected tagged JSON through that suite's comparer: integers and floats by value, offset date-times by instant,
/// the local kinds by value, and every fraction truncated to the 100-nanosecond tick Bodu keeps.
/// </para>
/// <para>
/// A <c>write</c> row replays its tagged JSON as calls on <see cref="Utf8TomlWriter" />, in the JSON's own order, and
/// compares the bytes written. A <c>roundtrip</c> row parses its input with <see cref="TomlDocument" />, writes the root
/// element back through <see cref="Utf8TomlWriter" />, reads that text again, and compares the two readings rendered as
/// canonical tagged JSON.
/// </para>
/// <para>
/// The <c>SpecVersion</c> option selects the TOML version the reader and the document parse under; the writer's output
/// is valid under both versions, so the writer takes only <c>MaxDepth</c>. Every row runs under a timeout, so an input
/// that makes a parser loop forever fails rather than hangs the run.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class TomlReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "toml";

    /// <summary>The time, in milliseconds, that one row may run before it fails.</summary>
    private const int RowTimeout = 10_000;

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames = ["SpecVersion", "MaxDepth"];

    /// <summary>The type tags a tagged-JSON scalar may carry, as toml-test spells them.</summary>
    private static readonly string[] s_typeTags =
    [
        "string", "integer", "float", "bool", "datetime", "datetime-local", "date-local", "time-local",
    ];

    /// <summary>The JSON reader options for tagged-JSON fields, deep enough for the catalogue's nesting-limit rows.</summary>
    private static readonly JsonDocumentOptions s_jsonOptions = new() { MaxDepth = 4096 };

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(TomlReleaseNoteCorpusTests).Assembly));

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
    /// Reads a document through <see cref="TomlDocumentReader" /> into the comparison model of the toml-test corpus
    /// suite.
    /// </summary>
    /// <param name="toml">The UTF-8 TOML bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>
    /// The model of the root table, as <see cref="TomlTestCorpusTests.BuildValue" /> builds it: tables are dictionaries,
    /// arrays are lists, and scalars are <see cref="TomlTestCorpusTests.Leaf" /> records.
    /// </returns>
    /// <exception cref="TomlFormatException">The reader rejects the document.</exception>
    private static object ReadModel(ReadOnlySpan<byte> toml, RowOptions options)
    {
        var reader = new TomlDocumentReader(toml, options.ToReaderOptions());
        Assert.IsTrue(reader.Read(), "The document produced no tokens.");

        return TomlTestCorpusTests.BuildValue(ref reader);
    }

    /// <summary>
    /// Renders a comparison model as compact, canonical toml-test tagged JSON.
    /// </summary>
    /// <param name="model">The comparison model.</param>
    /// <returns>
    /// The rendering, in printable ASCII: table keys in ordinal order, floats in their shortest round-trippable form or
    /// <c>nan</c>, <c>inf</c> and <c>-inf</c>, and date-times in RFC 3339 form with the fraction Bodu keeps.
    /// </returns>
    /// <remarks>
    /// Two readings of the same values render to the same text, so a round-trip row compares renderings; unlike the
    /// semantic comparer, the rendering also tells <c>0.0</c> from <c>-0.0</c> and keeps an offset date-time's offset.
    /// </remarks>
    private static string Render(object model)
    {
        var text = new StringBuilder();
        AppendRendering(text, model);
        return text.ToString();
    }

    /// <summary>
    /// Appends the rendering of a comparison model node and its descendants.
    /// </summary>
    /// <param name="text">The rendering so far.</param>
    /// <param name="model">The node.</param>
    /// <exception cref="InvalidOperationException">The node is not a table, an array or a scalar.</exception>
    private static void AppendRendering(StringBuilder text, object model)
    {
        switch (model)
        {
            case Dictionary<string, object> table:
                text.Append('{');
                bool first = true;
                foreach (string key in table.Keys.Order(StringComparer.Ordinal))
                {
                    if (!first)
                        text.Append(',');

                    first = false;
                    AppendJsonString(text, key);
                    text.Append(':');
                    AppendRendering(text, table[key]);
                }

                text.Append('}');
                break;

            case List<object> items:
                text.Append('[');
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                        text.Append(',');

                    AppendRendering(text, items[i]);
                }

                text.Append(']');
                break;

            case TomlTestCorpusTests.Leaf leaf:
                text.Append("{\"type\":\"").Append(TypeTag(leaf.Kind)).Append("\",\"value\":");
                AppendJsonString(text, FormatLeaf(leaf));
                text.Append('}');
                break;

            default:
                throw new InvalidOperationException($"The comparison model holds the unexpected node {model.GetType().Name}.");
        }
    }

    /// <summary>
    /// Gets the toml-test type tag of a scalar token type.
    /// </summary>
    /// <param name="kind">The scalar's token type.</param>
    /// <returns>The type tag, such as <c>integer</c> or <c>datetime-local</c>.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="kind" /> is not a scalar token type.</exception>
    private static string TypeTag(TomlTokenType kind) =>
        kind switch
        {
            TomlTokenType.String => "string",
            TomlTokenType.Integer => "integer",
            TomlTokenType.Float => "float",
            TomlTokenType.Boolean => "bool",
            TomlTokenType.OffsetDateTime => "datetime",
            TomlTokenType.LocalDateTime => "datetime-local",
            TomlTokenType.LocalDate => "date-local",
            TomlTokenType.LocalTime => "time-local",
            _ => throw new InvalidOperationException($"The token type {kind} is not a scalar."),
        };

    /// <summary>
    /// Formats a scalar's value as the text of its tagged-JSON <c>value</c>.
    /// </summary>
    /// <param name="leaf">The scalar.</param>
    /// <returns>The value text, culture-invariant.</returns>
    /// <exception cref="InvalidOperationException">The scalar holds a value of an unexpected type.</exception>
    private static string FormatLeaf(TomlTestCorpusTests.Leaf leaf) =>
        leaf.Value switch
        {
            string text => text,
            long integer => integer.ToString(CultureInfo.InvariantCulture),
            double number when double.IsNaN(number) => "nan",
            double number when double.IsPositiveInfinity(number) => "inf",
            double number when double.IsNegativeInfinity(number) => "-inf",
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            bool flag => flag ? "true" : "false",
            DateTimeOffset instant =>
                instant.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + FormatFraction(instant.Ticks) + FormatOffset(instant.Offset),
            DateTime local => local.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + FormatFraction(local.Ticks),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + FormatFraction(time.Ticks),
            _ => throw new InvalidOperationException($"The scalar holds the unexpected value type {leaf.Value.GetType().Name}."),
        };

    /// <summary>
    /// Formats the fractional second of a tick count.
    /// </summary>
    /// <param name="ticks">The tick count.</param>
    /// <returns>The fraction with a leading period and no trailing zero, or the empty string for a whole second.</returns>
    private static string FormatFraction(long ticks)
    {
        long fraction = ticks % TimeSpan.TicksPerSecond;
        return fraction == 0 ? string.Empty : "." + fraction.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
    }

    /// <summary>
    /// Formats a UTC offset as RFC 3339 writes it.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <returns><c>Z</c> for a zero offset; otherwise the sign followed by <c>hh:mm</c>.</returns>
    private static string FormatOffset(TimeSpan offset) =>
        offset == TimeSpan.Zero
            ? "Z"
            : (offset < TimeSpan.Zero ? "-" : "+") + offset.Duration().ToString("hh':'mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Appends a string as a JSON string literal in printable ASCII.
    /// </summary>
    /// <param name="text">The rendering so far.</param>
    /// <param name="value">The string.</param>
    /// <remarks>
    /// A quote, a backslash, a line feed, a carriage return and a tab use their short escapes, and every other character
    /// outside printable ASCII is written <c>\uXXXX</c>, one UTF-16 code unit at a time.
    /// </remarks>
    private static void AppendJsonString(StringBuilder text, string value)
    {
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
                    if (c is < ' ' or > '~')
                        text.Append(@"\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        text.Append(c);

                    break;
            }
        }

        text.Append('"');
    }

    /// <summary>
    /// Writes the value a tagged-JSON document describes through <see cref="Utf8TomlWriter" /> calls.
    /// </summary>
    /// <param name="json">The UTF-8 tagged JSON.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The bytes the writer produced.</returns>
    /// <remarks>
    /// An object is a table (<see cref="Utf8TomlWriter.WriteStartTable()" />, then
    /// <see cref="Utf8TomlWriter.WritePropertyName(string)" /> and the value for each member in order, then
    /// <see cref="Utf8TomlWriter.WriteEndTable()" />), an array an array, and a tagged scalar the matching typed write.
    /// </remarks>
    /// <exception cref="JsonException"><paramref name="json" /> is not JSON.</exception>
    /// <exception cref="FormatException">A scalar's value text does not parse as its type.</exception>
    private static byte[] WriteTaggedJson(byte[] json, RowOptions options)
    {
        using var document = JsonDocument.Parse(json, s_jsonOptions);

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer, options.ToWriterOptions());
        WriteElement(ref writer, document.RootElement);

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Writes one tagged-JSON element and its descendants.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="element">The element.</param>
    /// <exception cref="FormatException">The element is neither an object nor an array, or a scalar is malformed.</exception>
    private static void WriteElement(ref Utf8TomlWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object when TomlTestCorpusTests.TryGetLeafMarker(element, out string? type, out string? value):
                WriteLeaf(ref writer, type, value);
                break;

            case JsonValueKind.Object:
                writer.WriteStartTable();
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(ref writer, property.Value);
                }

                writer.WriteEndTable();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                    WriteElement(ref writer, item);

                writer.WriteEndArray();
                break;

            default:
                throw new FormatException($"The write notation has no {element.ValueKind} element.");
        }
    }

    /// <summary>
    /// Writes one tagged scalar with the typed write its type tag names.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="type">The type tag.</param>
    /// <param name="text">The value text.</param>
    /// <exception cref="FormatException">The type tag is unknown, or the text does not parse as its type.</exception>
    private static void WriteLeaf(ref Utf8TomlWriter writer, string type, string text)
    {
        switch (type)
        {
            case "string":
                writer.WriteString(text);
                break;

            case "integer":
                writer.WriteInteger(long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
                break;

            case "float":
                writer.WriteFloat(TomlTestCorpusTests.ParseExpectedFloat(text));
                break;

            case "bool":
                writer.WriteBoolean(ParseBoolean(text));
                break;

            case "datetime":
                writer.WriteOffsetDateTime(ParseOffsetDateTime(text));
                break;

            case "datetime-local":
                writer.WriteLocalDateTime(ParseLocalDateTime(text));
                break;

            case "date-local":
                writer.WriteLocalDate(ParseLocalDate(text));
                break;

            case "time-local":
                writer.WriteLocalTime(ParseLocalTime(text));
                break;

            default:
                throw new FormatException($"The write notation has no type tag '{type}'.");
        }
    }

    /// <summary>
    /// Checks that a field holds tagged JSON whose root is a table and whose every scalar carries a known type tag and
    /// a value that parses as that type.
    /// </summary>
    /// <param name="json">The UTF-8 field.</param>
    /// <exception cref="FormatException">The field is not tagged JSON of a table.</exception>
    private static void CheckTaggedTable(byte[] json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, s_jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"The field is not JSON: {ex.Message}", ex);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || TomlTestCorpusTests.TryGetLeafMarker(root, out _, out _))
                throw new FormatException("The tagged JSON does not describe a table at its root.");

            CheckTaggedElement(root);
        }
    }

    /// <summary>
    /// Checks one tagged-JSON element and its descendants.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <exception cref="FormatException">The element, or one below it, is not valid tagged JSON.</exception>
    private static void CheckTaggedElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object when TomlTestCorpusTests.TryGetLeafMarker(element, out string? type, out string? value):
                CheckLeaf(type, value);
                break;

            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                    CheckTaggedElement(property.Value);

                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                    CheckTaggedElement(item);

                break;

            default:
                throw new FormatException($"Tagged JSON has no {element.ValueKind} element.");
        }
    }

    /// <summary>
    /// Checks that a tagged scalar carries a known type tag and a value that parses as that type.
    /// </summary>
    /// <param name="type">The type tag.</param>
    /// <param name="text">The value text.</param>
    /// <exception cref="FormatException">The type tag is unknown, or the text does not parse as its type.</exception>
    private static void CheckLeaf(string type, string text)
    {
        if (!s_typeTags.Contains(type, StringComparer.Ordinal))
            throw new FormatException($"The type tag '{type}' is not one toml-test uses.");

        try
        {
            switch (type)
            {
                case "integer":
                    _ = long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                    break;

                case "float":
                    _ = TomlTestCorpusTests.ParseExpectedFloat(text);
                    break;

                case "bool":
                    _ = ParseBoolean(text);
                    break;

                case "datetime":
                    _ = ParseOffsetDateTime(text);
                    break;

                case "datetime-local":
                    _ = ParseLocalDateTime(text);
                    break;

                case "date-local":
                    _ = ParseLocalDate(text);
                    break;

                case "time-local":
                    _ = ParseLocalTime(text);
                    break;
            }
        }
        catch (OverflowException ex)
        {
            throw new FormatException($"The {type} value '{text}' is out of range.", ex);
        }
    }

    /// <summary>
    /// Parses a tagged-JSON Boolean value.
    /// </summary>
    /// <param name="text">The value text.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException"><paramref name="text" /> is neither <c>true</c> nor <c>false</c>.</exception>
    private static bool ParseBoolean(string text) =>
        text switch
        {
            "true" => true,
            "false" => false,
            _ => throw new FormatException($"The bool value '{text}' is neither true nor false."),
        };

    /// <summary>
    /// Parses a tagged-JSON offset date-time, truncating the fraction to the tick Bodu keeps.
    /// </summary>
    /// <param name="text">The value text.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException"><paramref name="text" /> is not a date-time with an offset.</exception>
    private static DateTimeOffset ParseOffsetDateTime(string text) =>
        DateTimeOffset.Parse(TomlTestCorpusTests.TruncateFraction(text), CultureInfo.InvariantCulture, DateTimeStyles.None);

    /// <summary>
    /// Parses a tagged-JSON local date-time, truncating the fraction to the tick Bodu keeps.
    /// </summary>
    /// <param name="text">The value text.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException"><paramref name="text" /> is not a date-time.</exception>
    private static DateTime ParseLocalDateTime(string text) =>
        DateTime.Parse(TomlTestCorpusTests.TruncateFraction(text), CultureInfo.InvariantCulture, DateTimeStyles.None);

    /// <summary>
    /// Parses a tagged-JSON local date.
    /// </summary>
    /// <param name="text">The value text.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException"><paramref name="text" /> is not a date.</exception>
    private static DateOnly ParseLocalDate(string text) =>
        DateOnly.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a tagged-JSON local time, truncating the fraction to the tick Bodu keeps.
    /// </summary>
    /// <param name="text">The value text.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException"><paramref name="text" /> is not a time of day.</exception>
    private static TimeOnly ParseLocalTime(string text) =>
        TimeOnly.Parse(TomlTestCorpusTests.TruncateFraction(text), CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a document with <see cref="TomlDocument" /> and writes its root element back through
    /// <see cref="Utf8TomlWriter" />.
    /// </summary>
    /// <param name="toml">The UTF-8 TOML bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The bytes the writer produced.</returns>
    /// <exception cref="TomlFormatException">The document is rejected.</exception>
    private static byte[] Rewrite(byte[] toml, RowOptions options)
    {
        using var document = TomlDocument.Parse(toml, options.ToDocumentOptions());

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer, options.ToWriterOptions());
        document.RootElement.WriteTo(writer);

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Describes an exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Its type name, its position when it carries one, and its message.</returns>
    private static string Describe(Exception exception)
    {
        string position = exception is TomlFormatException { LineNumber: int line, ColumnNumber: int column }
            ? string.Create(CultureInfo.InvariantCulture, $" at line {line}, column {column}")
            : string.Empty;
        return $"{exception.GetType().Name}{position}: {exception.Message}";
    }
}
