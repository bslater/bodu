// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MergeKeys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the YAML 1.1 merge key (<c>&lt;&lt;</c>) compatibility feature: importing keys without overriding
/// explicit ones, sequence-of-mappings precedence, explicit-key precedence over merged defaults, and the merging of a
/// source that has a merge key of its own.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a merge key imports keys from the merged mapping without overriding explicit keys.</summary>
    [TestMethod]
    public void Parse_WhenMergeKey_ShouldImportWithoutOverriding()
    {
        using var doc = YamlDocument.Parse(
            "defaults: &d\n  size: medium\n  color: red\nitem:\n  <<: *d\n  color: blue\n");
        YamlElement item = doc.RootElement.GetProperty("item");
        Assert.AreEqual("medium", item.GetProperty("size").GetString());
        Assert.AreEqual("blue", item.GetProperty("color").GetString());
        Assert.IsFalse(item.TryGetProperty("<<", out _));
    }

    /// <summary>Verifies that a merge key accepting a sequence of mappings applies earlier-source precedence.</summary>
    [TestMethod]
    public void Parse_WhenMergeSequence_ShouldApplyPrecedence()
    {
        using var doc = YamlDocument.Parse(
            "a: &a {x: 1, y: 1}\nb: &b {y: 2, z: 2}\nc:\n  <<: [*a, *b]\n");
        YamlElement c = doc.RootElement.GetProperty("c");
        Assert.AreEqual(1L, c.GetProperty("x").GetInt64());
        Assert.AreEqual(1L, c.GetProperty("y").GetInt64()); // first source wins
        Assert.AreEqual(2L, c.GetProperty("z").GetInt64());
    }

    /// <summary>Verifies that a merge key inserts defaults and an explicit key overrides them.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenMergeKeyAndExplicitKeyOverride_ShouldPreferExplicit()
    {
        using var doc = YamlDocument.Parse("defaults: &x\n  a: 1\n  b: 2\nconfig:\n  <<: *x\n  a: 999\n");
        Assert.AreEqual(999L, doc.RootElement.GetProperty("config").GetProperty("a").GetInt64());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("config").GetProperty("b").GetInt64());
    }

    /// <summary>
    /// Verifies that merging an aliased sequence of 3,200 empty mappings into each of 3,200 targets is rejected with
    /// <see cref="YamlFormatException" />: every merged alias counts toward the alias-expansion budget even when it folds
    /// no keys, so the quadratic merge work cannot grow without bound.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMergeSequencesOfEmptyMappingsAmplify_ShouldThrowYamlFormatException()
    {
        const int Count = 3_200;
        string yaml = "arr: &arr [" + string.Join(", ", Enumerable.Repeat("{}", Count)) + "]\ntargets:\n"
            + string.Concat(Enumerable.Repeat("  - <<: *arr\n", Count));
        var options = new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 };

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml, options);
        });
    }

    /// <summary>
    /// Verifies that a merge key whose value is not a mapping, an alias of one, or a sequence of them is rejected
    /// rather than dropped.
    /// </summary>
    /// <param name="yaml">A document with an invalid merge value.</param>
    [TestMethod]
    [DataRow("a: 1\n<<: 1\n", DisplayName = "a scalar")]
    [DataRow("a: 1\n<<:\n", DisplayName = "an empty value")]
    [DataRow("a: &x 5\n<<: *x\n", DisplayName = "an alias of a scalar")]
    [DataRow("<<: [ {a: 1}, 5 ]\n", DisplayName = "a sequence holding a scalar")]
    [DataRow("a: &x null\n<<:\n- *x\n", DisplayName = "a sequence holding an alias of null")]
    [DataRow("a: &x [abc]\n<<: *x\n", DisplayName = "an alias of a sequence holding a scalar")]
    [DataRow("a: &x [[p, q]]\n<<: *x\n", DisplayName = "an alias of a sequence holding a sequence")]
    [DataRow("{ a: 1, <<: 1 }", DisplayName = "a scalar in a flow mapping")]
    public void Parse_WhenMergeValueIsNotMapping_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that only the plain, untagged key <c>&lt;&lt;</c> is a merge key: a quoted or tagged <c>&lt;&lt;</c> is
    /// an ordinary key, beside a merge key as well as alone.
    /// </summary>
    /// <param name="yaml">A document with a quoted or tagged <c>&lt;&lt;</c> key.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("\"<<\": []\n", """{"\u003C\u003C":[]}""", DisplayName = "a double-quoted key")]
    [DataRow("'<<': {a: 1}\n", """{"\u003C\u003C":{"a":1}}""", DisplayName = "a single-quoted key")]
    [DataRow("a: &x {p: 1}\n\"<<\": *x\n", """{"a":{"p":1},"\u003C\u003C":{"p":1}}""", DisplayName = "aliased")]
    [DataRow("{ \"<<\": 1 }", """{"\u003C\u003C":1}""", DisplayName = "a quoted flow mapping key")]
    [DataRow("!!str <<: {a: 1}\n", """{"\u003C\u003C":{"a":1}}""", DisplayName = "a tagged key")]
    [DataRow("\"<<\": 1\n<<: {a: 2}\n", """{"\u003C\u003C":1,"a":2}""", DisplayName = "beside a merge key")]
    public void Parse_WhenMergeKeyIsQuotedOrTagged_ShouldReadOrdinaryKey(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        // The JSON rendering escapes each '<' of the key as \u003C.
        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>A document whose mapping <c>c</c> holds two merge keys.</summary>
    private const string TwoMergeKeysYaml = "a: &a {p: 1}\nb: &b {q: 2}\nc:\n  <<: *a\n  <<: *b\n";

    /// <summary>
    /// Verifies that a mapping with more than one merge key is rejected whatever the duplicate-key policy, since
    /// several merges are written as one merge key with a sequence.
    /// </summary>
    /// <param name="yaml">A document whose mapping has two merge keys.</param>
    /// <param name="policy">The duplicate-key policy the document is read under.</param>
    [TestMethod]
    [DataRow(TwoMergeKeysYaml, YamlDuplicateKeyBehavior.Throw, DisplayName = "Throw")]
    [DataRow(TwoMergeKeysYaml, YamlDuplicateKeyBehavior.UseFirst, DisplayName = "UseFirst")]
    [DataRow(TwoMergeKeysYaml, YamlDuplicateKeyBehavior.UseLast, DisplayName = "UseLast")]
    [DataRow("{ <<: {a: 1}, <<: {b: 2} }", YamlDuplicateKeyBehavior.UseLast, DisplayName = "a flow mapping, UseLast")]
    public void Parse_WhenMappingHasTwoMergeKeys_ShouldThrowYamlFormatException(
        string yaml,
        YamlDuplicateKeyBehavior policy)
    {
        var options = new YamlDocumentOptions { DuplicateKeyBehavior = policy };

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml, options);
        });
    }

    /// <summary>
    /// Verifies that a merge source with a merge key of its own contributes the keys that merge supplies, and no merge
    /// key, wherever the source is written, so the explicit keys of each mapping still win over its merged ones and an
    /// earlier source in a sequence still wins over a later one.
    /// </summary>
    /// <param name="yaml">A document whose mapping <c>outer</c> merges a source that merges in turn.</param>
    /// <param name="expected">The entries of <c>outer</c>, sorted by key, each value in its JSON form.</param>
    [TestMethod]
    [DataRow(
        "outer:\n  <<: &inner\n    <<: {x: 1}\n    y: 2\n  z: 3\n",
        "x=1,y=2,z=3",
        DisplayName = "a source written inside the mapping that merges it")]
    [DataRow(
        "outer:\n  <<: [ {<<: {x: 1}, y: 2}, {<<: {w: 0}, z: 3} ]\n",
        "w=0,x=1,y=2,z=3",
        DisplayName = "a sequence of sources that each merge")]
    [DataRow(
        "outer:\n  <<: &m\n    <<: &n\n      <<: {x: 1}\n      y: 2\n    z: 3\n  w: 4\n",
        "w=4,x=1,y=2,z=3",
        DisplayName = "three levels of merges")]
    [DataRow(
        "outer:\n  <<:\n    <<: {a: merged, b: merged}\n    b: source\n  a: explicit\n",
        "a=\"explicit\",b=\"source\"",
        DisplayName = "explicit keys winning at each level")]
    [DataRow(
        "outer:\n  <<: [ {<<: {k: first}}, {k: second} ]\n",
        "k=\"first\"",
        DisplayName = "an earlier source winning with a key its own merge supplies")]
    [DataRow(
        "base: &b {<<: {x: 1}, y: 2}\nouter:\n  <<: *b\n  z: 3\n",
        "x=1,y=2,z=3",
        DisplayName = "an aliased source written earlier in the document")]
    public void Parse_WhenMergeSourceHasMergeKey_ShouldMergeItsMergedKeys(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        YamlElement outer = document.RootElement.GetProperty("outer");
        string entries = string.Join(
            ",",
            outer.EnumerateMapping().Select(property => property.Name + "=" + ToJson(property.Value)).Order(StringComparer.Ordinal));
        Assert.AreEqual(expected, entries);
    }
}
