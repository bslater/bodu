// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MappingKeys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies how mapping keys are named: a scalar key is named by its text as written in every key form (implicit block
/// keys, flow mapping keys, explicit keys and flow-sequence pairs), whatever value the schema resolves the text to.
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
}
