// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MappingKeys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the mapping key contracts: a scalar key is named by its text as written in every key form (implicit block
/// keys, flow mapping keys, explicit keys and flow-sequence pairs), whatever value the schema resolves the text to, an
/// implicit key holds at most 1024 characters, and a key that is not a scalar is rejected where it begins.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>The keys of the YAML 1.2 key-naming documents, each named by its text, as JSON.</summary>
    private const string CoreSchemaKeysJson = """{"0x10":"a","1.0":"b","True":"c","~":"d","1e3":"e"}""";

    /// <summary>The pairs of the YAML 1.2 key-naming flow sequence, each key named by its text, as JSON.</summary>
    private const string CoreSchemaPairsJson = """[{"0x10":"a"},{"1.0":"b"},{"True":"c"},{"~":"d"},{"1e3":"e"}]""";

    /// <summary>The keys of the YAML 1.1 key-naming documents, each named by its text, as JSON.</summary>
    private const string Yaml11KeysJson = """{"y":"a","on":"b","010":"c","0b11":"d","1_000":"e"}""";

    /// <summary>
    /// Verifies that a plain key whose text resolves to an integer, a float, a boolean or null is named by its text in
    /// every key form, as an implicit block key is, under both schemas.
    /// </summary>
    /// <param name="yaml">A document whose keys resolve to values other than strings.</param>
    /// <param name="version">The specification version the document is read under.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow(
        "0x10: a\n1.0: b\nTrue: c\n~: d\n1e3: e\n",
        YamlSpecVersion.V1_2,
        CoreSchemaKeysJson,
        DisplayName = "block keys")]
    [DataRow(
        "{ 0x10: a, 1.0: b, True: c, ~: d, 1e3: e }",
        YamlSpecVersion.V1_2,
        CoreSchemaKeysJson,
        DisplayName = "flow keys")]
    [DataRow(
        "{ ? 0x10 : a, ? 1.0 : b, ? True : c, ? ~ : d, ? 1e3 : e }",
        YamlSpecVersion.V1_2,
        CoreSchemaKeysJson,
        DisplayName = "explicit flow keys")]
    [DataRow(
        "? 0x10\n: a\n? 1.0\n: b\n? True\n: c\n? ~\n: d\n? 1e3\n: e\n",
        YamlSpecVersion.V1_2,
        CoreSchemaKeysJson,
        DisplayName = "explicit block keys")]
    [DataRow(
        "[ 0x10: a, 1.0: b, True: c, ~: d, 1e3: e ]",
        YamlSpecVersion.V1_2,
        CoreSchemaPairsJson,
        DisplayName = "flow sequence pairs")]
    [DataRow(
        "y: a\non: b\n010: c\n0b11: d\n1_000: e\n",
        YamlSpecVersion.V1_1,
        Yaml11KeysJson,
        DisplayName = "YAML 1.1 block keys")]
    [DataRow(
        "{ y: a, on: b, 010: c, 0b11: d, 1_000: e }",
        YamlSpecVersion.V1_1,
        Yaml11KeysJson,
        DisplayName = "YAML 1.1 flow keys")]
    [DataRow(
        "? y\n: a\n? on\n: b\n? 010\n: c\n? 0b11\n: d\n? 1_000\n: e\n",
        YamlSpecVersion.V1_1,
        Yaml11KeysJson,
        DisplayName = "YAML 1.1 explicit block keys")]
    public void Parse_WhenKeyResolvesToValue_ShouldNameKeyByText(string yaml, YamlSpecVersion version, string expected)
    {
        using var document = YamlDocument.Parse(yaml, new YamlDocumentOptions { SpecVersion = version });

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that keys spelled differently are distinct even when they resolve to the same value, so a flow or
    /// explicit key never collides with a key it does not spell.
    /// </summary>
    /// <param name="yaml">A document whose keys resolve to the same value.</param>
    /// <param name="version">The specification version the document is read under.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("{ x: 1, y: 2 }", YamlSpecVersion.V1_1, """{"x":1,"y":2}""", DisplayName = "y beside x, YAML 1.1")]
    [DataRow("{ y: 2, yes: 3 }", YamlSpecVersion.V1_1, """{"y":2,"yes":3}""", DisplayName = "y beside yes, YAML 1.1")]
    [DataRow("? y\n: 2\n? yes\n: 3\n", YamlSpecVersion.V1_1, """{"y":2,"yes":3}""", DisplayName = "explicit y, yes")]
    [DataRow("{ 0x10: a, 16: b }", YamlSpecVersion.V1_2, """{"0x10":"a","16":"b"}""", DisplayName = "0x10 beside 16")]
    [DataRow("{ 1.0: a, 1: b }", YamlSpecVersion.V1_2, """{"1.0":"a","1":"b"}""", DisplayName = "1.0 beside 1")]
    [DataRow("{ ~: a, null: b }", YamlSpecVersion.V1_2, """{"~":"a","null":"b"}""", DisplayName = "~ beside null")]
    public void Parse_WhenKeysResolveAlike_ShouldStayDistinct(string yaml, YamlSpecVersion version, string expected)
    {
        using var document = YamlDocument.Parse(yaml, new YamlDocumentOptions { SpecVersion = version });

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that an implicit key of 1024 characters, counted with its node properties, its quotes and the spaces
    /// before its value indicator, is read, in a block mapping and in a flow-sequence pair alike.
    /// </summary>
    /// <param name="template">The document, in which <c>{0}</c> stands for the key's run of characters.</param>
    /// <param name="unit">The character the run repeats.</param>
    /// <param name="count">The number of characters in the run.</param>
    [TestMethod]
    [DataRow("{0}: v\n", "x", 1024, DisplayName = "a plain key")]
    [DataRow("\"{0}\": v\n", "x", 1022, DisplayName = "a quoted key, with its quotes")]
    [DataRow("{0} : v\n", "x", 1023, DisplayName = "a plain key, with a space before its indicator")]
    [DataRow("a: 1\n{0}: v\n", "x", 1024, DisplayName = "a later key")]
    [DataRow("- {0}: v\n", "x", 1024, DisplayName = "a compact mapping key")]
    [DataRow("&a {0}: v\n", "x", 1021, DisplayName = "an anchored key, with its anchor")]
    [DataRow("[ {0}: v ]", "x", 1024, DisplayName = "a flow-sequence pair key")]
    [DataRow("{0}: v\n", "\u00E9", 1024, DisplayName = "two-byte characters")]
    [DataRow("{0}: v\n", "\uD83D\uDE00", 1024, DisplayName = "characters outside the Basic Multilingual Plane")]
    public void Parse_WhenImplicitKeyHas1024Characters_ShouldReadIt(string template, string unit, int count)
    {
        string key = string.Concat(Enumerable.Repeat(unit, count));
        using var document = YamlDocument.Parse(template.Replace("{0}", key, StringComparison.Ordinal));

        YamlElement mapping = document.RootElement.ValueKind == YamlValueKind.Sequence
            ? document.RootElement[0]
            : document.RootElement;
        Assert.AreEqual("v", mapping.GetProperty(key).GetString());
    }

    /// <summary>
    /// Verifies that an implicit key longer than 1024 characters, counted with its node properties, its quotes and the
    /// spaces before its value indicator, is rejected, in a block mapping and in a flow-sequence pair alike.
    /// </summary>
    /// <param name="template">The document, in which <c>{0}</c> stands for the key's run of characters.</param>
    /// <param name="unit">The character the run repeats.</param>
    /// <param name="count">The number of characters in the run.</param>
    [TestMethod]
    [DataRow("{0}: v\n", "x", 1025, DisplayName = "a plain key")]
    [DataRow("\"{0}\": v\n", "x", 1023, DisplayName = "a quoted key, with its quotes")]
    [DataRow("{0} : v\n", "x", 1024, DisplayName = "a plain key, with a space before its indicator")]
    [DataRow("a: 1\n{0}: v\n", "x", 1025, DisplayName = "a later key")]
    [DataRow("- {0}: v\n", "x", 1025, DisplayName = "a compact mapping key")]
    [DataRow("&a {0}: v\n", "x", 1022, DisplayName = "an anchored key, with its anchor")]
    [DataRow("[ {0}: v ]", "x", 1025, DisplayName = "a flow-sequence pair key")]
    [DataRow("{0}: v\n", "\u00E9", 1025, DisplayName = "two-byte characters")]
    [DataRow("{0}: v\n", "\uD83D\uDE00", 1025, DisplayName = "characters outside the Basic Multilingual Plane")]
    public void Parse_WhenImplicitKeyExceeds1024Characters_ShouldThrowYamlFormatException(
        string template,
        string unit,
        int count)
    {
        string key = string.Concat(Enumerable.Repeat(unit, count));

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(template.Replace("{0}", key, StringComparison.Ordinal));
        });
    }

    /// <summary>
    /// Verifies that a sequence, mapping, or alias used as a flow mapping key, a flow-pair key or an explicit key is
    /// rejected with the complex-key message where the key begins, at its first node property when it has one, with its
    /// line, column and offset, rather than after the key.
    /// </summary>
    /// <param name="yaml">A document with a key that is not a scalar.</param>
    /// <param name="line">The line on which the key begins.</param>
    /// <param name="column">The column at which the key begins.</param>
    /// <param name="offset">The byte offset at which the key begins.</param>
    [TestMethod]
    [DataRow("a: &k key\nb: {*k : v}\n", 2, 5, 14, DisplayName = "an alias as a flow mapping key")]
    [DataRow("a: &k key\nb: {\"q\" : v, *k : w}\n", 2, 14, 23, DisplayName = "an alias as a later flow mapping key")]
    [DataRow("{[1] : v}\n", 1, 2, 1, DisplayName = "a flow sequence as a flow mapping key")]
    [DataRow("a: &k key\n? *k\n: v\n", 2, 3, 12, DisplayName = "an alias as an explicit key")]
    [DataRow("a: &k key\nb: [*k : v]\n", 2, 5, 14, DisplayName = "an alias as a flow-pair key")]
    [DataRow("a: &k key\nb: {? *k : v}\n", 2, 7, 16, DisplayName = "an alias as an explicit flow mapping key")]
    [DataRow("{&a [1] : v}\n", 1, 2, 1, DisplayName = "an anchored flow sequence as a flow mapping key")]
    [DataRow("[&a {x: 1} : v]\n", 1, 2, 1, DisplayName = "an anchored flow mapping as a flow-pair key")]
    [DataRow("? &a [1]\n: v\n", 1, 3, 2, DisplayName = "an anchored flow sequence as an explicit key")]
    [DataRow("?\n  &a [1]\n: v\n", 2, 3, 4, DisplayName = "an explicit key on the line after its indicator")]
    public void Parse_WhenKeyIsNotScalar_ShouldReportComplexKeyWhereKeyBegins(string yaml, int line, int column, int offset)
    {
        YamlFormatException ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });

        Assert.AreEqual(YamlResourceStrings.Format_Invalid_YamlComplexKeyUnsupported, ex.Message);
        Assert.AreEqual(line, ex.LineNumber);
        Assert.AreEqual(column, ex.ColumnNumber);
        Assert.AreEqual(offset, ex.Offset);
    }
}
