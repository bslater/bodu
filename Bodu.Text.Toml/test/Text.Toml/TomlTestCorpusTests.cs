// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTestCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Runs the vendored <c>toml-lang/toml-test</c> conformance corpus against <see cref="TomlDocumentReader" /> in both the
/// TOML v1.0.0 and v1.1.0 profiles: every valid document must parse and decode to the values pinned by its JSON
/// expectation file, and every invalid document must be rejected with <see cref="TomlFormatException" />.
/// </summary>
/// <remarks>
/// <para>
/// The corpus lives under <c>TomlTestCorpus/</c> (copied to the test output) together with its upstream MIT licence.
/// The per-version case lists (<c>files-toml-1.0.0</c> / <c>files-toml-1.1.0</c>) published by the corpus select which
/// cases apply to each specification profile, so v1.1.0-only grammar appears as valid in the v1.1 run and as invalid
/// in the v1.0 run.
/// </para>
/// <para>
/// Valid-case expectations use the corpus's tagged-JSON encoding, where a leaf value is an object of exactly
/// <c>{"type": …, "value": …}</c>; comparison is semantic per type (numeric for integers and floats, instant-based for
/// date-times) rather than textual.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class TomlTestCorpusTests
{
    /// <summary>
    /// The root directory of the vendored corpus within the test output.
    /// </summary>
    private static readonly string CorpusRoot = Path.Combine(AppContext.BaseDirectory, "TomlTestCorpus");

    /// <summary>
    /// Corpus cases excluded from the run, keyed by relative path with the documented reason as the value.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> SkippedCases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // Currently empty: no corpus case conflicts with the library's documented CLR representational limits.
    };

    /// <summary>
    /// Yields the valid corpus cases for the TOML v1.0.0 profile.
    /// </summary>
    /// <returns>One row per valid v1.0.0 case.</returns>
    public static IEnumerable<object[]> ValidCasesV10() =>
        EnumerateCases("files-toml-1.0.0", TomlSpecVersion.V1_0, valid: true);

    /// <summary>
    /// Yields the valid corpus cases for the TOML v1.1.0 profile.
    /// </summary>
    /// <returns>One row per valid v1.1.0 case.</returns>
    public static IEnumerable<object[]> ValidCasesV11() =>
        EnumerateCases("files-toml-1.1.0", TomlSpecVersion.V1_1, valid: true);

    /// <summary>
    /// Yields the invalid corpus cases for the TOML v1.0.0 profile.
    /// </summary>
    /// <returns>One row per invalid v1.0.0 case.</returns>
    public static IEnumerable<object[]> InvalidCasesV10() =>
        EnumerateCases("files-toml-1.0.0", TomlSpecVersion.V1_0, valid: false);

    /// <summary>
    /// Yields the invalid corpus cases for the TOML v1.1.0 profile.
    /// </summary>
    /// <returns>One row per invalid v1.1.0 case.</returns>
    public static IEnumerable<object[]> InvalidCasesV11() =>
        EnumerateCases("files-toml-1.1.0", TomlSpecVersion.V1_1, valid: false);

    /// <summary>
    /// Verifies that every valid v1.0.0 corpus document parses and decodes to the values pinned by its JSON
    /// expectation.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV10), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenCorpusDocumentIsValid_ForV10_ShouldMatchExpectedValues(CorpusKat kat) =>
        AssertValidCase(kat);

    /// <summary>
    /// Verifies that every valid v1.1.0 corpus document parses and decodes to the values pinned by its JSON
    /// expectation.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV11), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenCorpusDocumentIsValid_ForV11_ShouldMatchExpectedValues(CorpusKat kat) =>
        AssertValidCase(kat);

    /// <summary>
    /// Verifies that every invalid v1.0.0 corpus document is rejected with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidCasesV10), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Constructor_WhenCorpusDocumentIsInvalid_ForV10_ShouldThrowTomlFormatException(CorpusKat kat) =>
        AssertInvalidCase(kat);

    /// <summary>
    /// Verifies that every invalid v1.1.0 corpus document is rejected with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidCasesV11), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Constructor_WhenCorpusDocumentIsInvalid_ForV11_ShouldThrowTomlFormatException(CorpusKat kat) =>
        AssertInvalidCase(kat);

    /// <summary>
    /// Asserts that a valid corpus document parses and matches its JSON expectation.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    private static void AssertValidCase(CorpusKat kat)
    {
        byte[] toml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath));
        string expectationPath = Path.Combine(CorpusRoot, Path.ChangeExtension(kat.RelativePath, ".json"));

        var reader = new TomlDocumentReader(toml, new TomlReaderOptions { SpecVersion = kat.SpecVersion });
        Assert.IsTrue(reader.Read(), $"{kat.RelativePath}: the document produced no tokens.");
        object actual = BuildValue(ref reader);

        using var expected = JsonDocument.Parse(File.ReadAllBytes(expectationPath));
        AssertMatches(expected.RootElement, actual, kat.RelativePath);
    }

    /// <summary>
    /// Asserts that an invalid corpus document is rejected with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    private static void AssertInvalidCase(CorpusKat kat)
    {
        byte[] toml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath));

        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new TomlDocumentReader(toml, new TomlReaderOptions { SpecVersion = kat.SpecVersion });
        });

        Assert.IsNotNull(ex.Message);
    }

    /// <summary>
    /// Reads the corpus case list and yields the rows matching the requested validity.
    /// </summary>
    /// <param name="listFileName">The per-version case list file name.</param>
    /// <param name="specVersion">The specification profile the list applies to.</param>
    /// <param name="valid">Whether to yield valid or invalid cases.</param>
    /// <returns>One row per selected case.</returns>
    private static IEnumerable<object[]> EnumerateCases(string listFileName, TomlSpecVersion specVersion, bool valid)
    {
        foreach (string line in File.ReadLines(Path.Combine(CorpusRoot, listFileName)))
        {
            if (!line.EndsWith(".toml", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("valid/", StringComparison.Ordinal) != valid)
                continue;

            if (SkippedCases.ContainsKey(line))
                continue;

            yield return [new CorpusKat(line, specVersion)];
        }
    }

    /// <summary>
    /// Builds a comparison model for the value at the reader's position: tables become dictionaries, arrays become
    /// lists, and scalars become <see cref="Leaf" /> records carrying their token type.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value's first token.</param>
    /// <returns>The comparison model.</returns>
    private static object BuildValue(ref TomlDocumentReader reader)
    {
        switch (reader.TokenType)
        {
            case TomlTokenType.StartTable:
                var table = new Dictionary<string, object>(StringComparer.Ordinal);
                while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
                {
                    string key = reader.GetString();
                    Assert.IsTrue(reader.Read(), "A property name must be followed by a value.");
                    table[key] = BuildValue(ref reader);
                }

                return table;

            case TomlTokenType.StartArray:
                var items = new List<object>();
                while (reader.Read() && reader.TokenType != TomlTokenType.EndArray)
                    items.Add(BuildValue(ref reader));

                return items;

            case TomlTokenType.String:
                return new Leaf(TomlTokenType.String, reader.GetString());
            case TomlTokenType.Integer:
                return new Leaf(TomlTokenType.Integer, reader.GetInt64());
            case TomlTokenType.Float:
                return new Leaf(TomlTokenType.Float, reader.GetDouble());
            case TomlTokenType.Boolean:
                return new Leaf(TomlTokenType.Boolean, reader.GetBoolean());
            case TomlTokenType.OffsetDateTime:
                return new Leaf(TomlTokenType.OffsetDateTime, reader.GetDateTimeOffset());
            case TomlTokenType.LocalDateTime:
                return new Leaf(TomlTokenType.LocalDateTime, reader.GetDateTime());
            case TomlTokenType.LocalDate:
                return new Leaf(TomlTokenType.LocalDate, reader.GetDateOnly());
            default:
                return new Leaf(TomlTokenType.LocalTime, reader.GetTimeOnly());
        }
    }

    /// <summary>
    /// Asserts that the parsed model matches the tagged-JSON expectation element.
    /// </summary>
    /// <param name="expected">The expectation element.</param>
    /// <param name="actual">The parsed comparison model.</param>
    /// <param name="path">The dotted location within the document, used in failure messages.</param>
    private static void AssertMatches(JsonElement expected, object actual, string path)
    {
        if (expected.ValueKind == JsonValueKind.Object)
        {
            if (TryGetLeafMarker(expected, out string? type, out string? value))
            {
                AssertLeaf(type, value, actual, path);
                return;
            }

            Assert.IsInstanceOfType<Dictionary<string, object>>(actual, $"{path}: expected a table.");
            var table = (Dictionary<string, object>)actual;

            int expectedCount = 0;
            foreach (JsonProperty property in expected.EnumerateObject())
            {
                expectedCount++;
                Assert.IsTrue(table.TryGetValue(property.Name, out object? child), $"{path}: missing key '{property.Name}'.");
                AssertMatches(property.Value, child!, $"{path}.{property.Name}");
            }

            Assert.HasCount(expectedCount, table, $"{path}: key count.");
            return;
        }

        if (expected.ValueKind == JsonValueKind.Array)
        {
            Assert.IsInstanceOfType<List<object>>(actual, $"{path}: expected an array.");
            var list = (List<object>)actual;

            Assert.HasCount(expected.GetArrayLength(), list, $"{path}: array length.");
            int index = 0;
            foreach (JsonElement element in expected.EnumerateArray())
            {
                AssertMatches(element, list[index], $"{path}[{index}]");
                index++;
            }

            return;
        }

        Assert.Fail($"{path}: unsupported expectation element of kind {expected.ValueKind}.");
    }

    /// <summary>
    /// Detects the corpus's tagged leaf encoding: an object of exactly <c>{"type": …, "value": …}</c> with string
    /// values.
    /// </summary>
    /// <param name="element">The expectation element to inspect.</param>
    /// <param name="type">When the element is a leaf marker, its type tag.</param>
    /// <param name="value">When the element is a leaf marker, its value text.</param>
    /// <returns><see langword="true" /> when the element is a leaf marker; otherwise <see langword="false" />.</returns>
    private static bool TryGetLeafMarker(JsonElement element, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? type, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
    {
        type = null;
        value = null;

        int count = 0;
        string? typeText = null;
        string? valueText = null;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            count++;
            if (property.NameEquals("type") && property.Value.ValueKind == JsonValueKind.String)
                typeText = property.Value.GetString();
            else if (property.NameEquals("value") && property.Value.ValueKind == JsonValueKind.String)
                valueText = property.Value.GetString();
        }

        if (count != 2 || typeText is null || valueText is null)
            return false;

        type = typeText;
        value = valueText;
        return true;
    }

    /// <summary>
    /// Asserts that a parsed scalar matches a tagged leaf expectation, comparing semantically per type.
    /// </summary>
    /// <param name="type">The expectation's type tag.</param>
    /// <param name="text">The expectation's value text.</param>
    /// <param name="actual">The parsed comparison model.</param>
    /// <param name="path">The dotted location within the document, used in failure messages.</param>
    private static void AssertLeaf(string type, string text, object actual, string path)
    {
        Assert.IsInstanceOfType<Leaf>(actual, $"{path}: expected a {type} scalar.");
        var leaf = (Leaf)actual;

        switch (type)
        {
            case "string":
                Assert.AreEqual(TomlTokenType.String, leaf.Kind, $"{path}: token kind.");
                Assert.AreEqual(text, (string)leaf.Value, $"{path}: string value.");
                break;

            case "integer":
                Assert.AreEqual(TomlTokenType.Integer, leaf.Kind, $"{path}: token kind.");
                Assert.AreEqual(long.Parse(text, CultureInfo.InvariantCulture), (long)leaf.Value, $"{path}: integer value.");
                break;

            case "float":
                Assert.AreEqual(TomlTokenType.Float, leaf.Kind, $"{path}: token kind.");
                Assert.AreEqual(ParseExpectedFloat(text), (double)leaf.Value, $"{path}: float value.");
                break;

            case "bool":
                Assert.AreEqual(TomlTokenType.Boolean, leaf.Kind, $"{path}: token kind.");
                Assert.AreEqual(bool.Parse(text), (bool)leaf.Value, $"{path}: bool value.");
                break;

            case "datetime":
                Assert.AreEqual(TomlTokenType.OffsetDateTime, leaf.Kind, $"{path}: token kind.");
                var expectedOffset = DateTimeOffset.Parse(TruncateFraction(text), CultureInfo.InvariantCulture, DateTimeStyles.None);
                Assert.AreEqual(expectedOffset.UtcTicks, ((DateTimeOffset)leaf.Value).UtcTicks, $"{path}: offset date-time instant.");
                break;

            case "datetime-local":
                Assert.AreEqual(TomlTokenType.LocalDateTime, leaf.Kind, $"{path}: token kind.");
                var expectedLocal = DateTime.Parse(TruncateFraction(text), CultureInfo.InvariantCulture, DateTimeStyles.None);
                Assert.AreEqual(expectedLocal.Ticks, ((DateTime)leaf.Value).Ticks, $"{path}: local date-time value.");
                break;

            case "date-local":
                Assert.AreEqual(TomlTokenType.LocalDate, leaf.Kind, $"{path}: token kind.");
                Assert.AreEqual(DateOnly.Parse(text, CultureInfo.InvariantCulture), (DateOnly)leaf.Value, $"{path}: local date value.");
                break;

            case "time-local":
                Assert.AreEqual(TomlTokenType.LocalTime, leaf.Kind, $"{path}: token kind.");
                var expectedTime = TimeOnly.Parse(TruncateFraction(text), CultureInfo.InvariantCulture);
                Assert.AreEqual(expectedTime.Ticks, ((TimeOnly)leaf.Value).Ticks, $"{path}: local time value.");
                break;

            default:
                Assert.Fail($"{path}: unsupported expectation type tag '{type}'.");
                break;
        }
    }

    /// <summary>
    /// Parses an expectation float, honoring the corpus spellings of the non-finite sentinels.
    /// </summary>
    /// <param name="text">The expectation value text.</param>
    /// <returns>The parsed double.</returns>
    private static double ParseExpectedFloat(string text) =>
        text switch
        {
            "inf" or "+inf" => double.PositiveInfinity,
            "-inf" => double.NegativeInfinity,
            "nan" or "+nan" or "-nan" => double.NaN,
            _ => double.Parse(text, CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Truncates a fractional-second component beyond the CLR's seven-digit tick precision, mirroring the truncation
    /// the specification requires of parsers.
    /// </summary>
    /// <param name="text">The date-time expectation text.</param>
    /// <returns>The text with at most seven fractional-second digits.</returns>
    private static string TruncateFraction(string text)
    {
        int dot = text.IndexOf('.', StringComparison.Ordinal);
        if (dot < 0)
            return text;

        int end = dot + 1;
        while (end < text.Length && char.IsAsciiDigit(text[end]))
            end++;

        int digits = end - dot - 1;
        return digits <= 7 ? text : text.Remove(dot + 8, digits - 7);
    }

    /// <summary>
    /// A corpus case row: the document's relative path and the specification profile it runs under.
    /// </summary>
    /// <param name="RelativePath">The case path relative to the corpus root.</param>
    /// <param name="SpecVersion">The specification profile the case runs under.</param>
    public sealed record CorpusKat(string RelativePath, TomlSpecVersion SpecVersion)
        : IKat
    {
        /// <inheritdoc />
        public string Name =>
            $"[{(SpecVersion == TomlSpecVersion.V1_1 ? "v1.1" : "v1.0")}] {RelativePath}";
    }

    /// <summary>
    /// A parsed scalar in the comparison model, pairing the token type with the decoded value.
    /// </summary>
    /// <param name="Kind">The scalar's token type.</param>
    /// <param name="Value">The decoded scalar value.</param>
    private sealed record Leaf(TomlTokenType Kind, object Value);
}
