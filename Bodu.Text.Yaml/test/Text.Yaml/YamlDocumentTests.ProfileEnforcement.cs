// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.ProfileEnforcement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that the parser enforces the library's YAML 1.2 core, JSON-compatible tree profile by rejecting the
/// constructs that the profile excludes: duplicate keys, complex keys, duplicate anchors, tabs in indentation, invalid
/// UTF-8, non-printable control characters, invalid Unicode escapes, and nesting beyond the configured depth.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>
    /// Verifies that a block mapping which repeats a key is rejected with <see cref="YamlFormatException" /> under the
    /// default duplicate-key policy.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBlockMappingHasDuplicateKey_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: 1\na: 2\n");
        });
    }

    /// <summary>
    /// Verifies that a flow mapping which repeats a key is rejected with <see cref="YamlFormatException" /> under the
    /// default duplicate-key policy.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFlowMappingHasDuplicateKey_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("{a: 1, a: 2}\n");
        });
    }

    /// <summary>
    /// Verifies that the <see cref="YamlDuplicateKeyBehavior.UseFirst" /> policy keeps the first occurrence of a
    /// duplicated key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyAndUseFirst_ShouldKeepFirst()
    {
        using var doc = YamlDocument.Parse(
            Encoding.UTF8.GetBytes("a: 1\na: 2\n"),
            new YamlDocumentOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseFirst });

        Assert.AreEqual(1L, doc.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>
    /// Verifies that the <see cref="YamlDuplicateKeyBehavior.UseLast" /> policy keeps the last occurrence of a
    /// duplicated key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyAndUseLast_ShouldKeepLast()
    {
        using var doc = YamlDocument.Parse(
            Encoding.UTF8.GetBytes("a: 1\na: 2\n"),
            new YamlDocumentOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseLast });

        Assert.AreEqual(2L, doc.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>
    /// Verifies that an explicit complex mapping key (a sequence) is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenExplicitComplexKey_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("? [a, b]\n: value\n");
        });
    }

    /// <summary>
    /// Verifies that a flow mapping with a sequence key is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFlowComplexKey_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("{[a]: b}\n");
        });
    }

    /// <summary>
    /// Verifies that defining the same anchor twice is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAnchorRedefined_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: &x 1\nb: &x 2\n");
        });
    }

    /// <summary>
    /// Verifies that a tab used to indent a structural content line is rejected with
    /// <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTabIndentation_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a:\n\tb: 1\n");
        });
    }

    /// <summary>
    /// Verifies that a tab used as separation after a value indicator is accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTabSeparatesValue_ShouldAccept()
    {
        using var doc = YamlDocument.Parse("a:\tvalue\n");

        Assert.AreEqual("value", doc.RootElement.GetProperty("a").GetString());
    }

    /// <summary>
    /// Verifies that a tab after a node property, separating it from what follows on the same line, is separation and
    /// is accepted.
    /// </summary>
    /// <param name="yaml">A document with a tab after a node property.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("- !!str\ta", """["a"]""", DisplayName = "after a tag in a sequence entry")]
    [DataRow("%TAG\t!e!\ttag:yaml.org,2002:\n--- !e!str\ta", "\"a\"", DisplayName = "after a %TAG-expanded tag")]
    [DataRow("a: !!str\t1", """{"a":"1"}""", DisplayName = "after a tag on a mapping value")]
    [DataRow("&x\ta", "\"a\"", DisplayName = "after an anchor on the root")]
    [DataRow("- &x\t!!str\ta", """["a"]""", DisplayName = "between two properties and after them")]
    public void Parse_WhenTabFollowsNodeProperty_ShouldAccept(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a tab between a sequence entry's dash and a compact mapping or sequence on its line is rejected,
    /// since the tab would indent the nested collection.
    /// </summary>
    /// <param name="yaml">A sequence entry whose compact collection follows a tab.</param>
    [TestMethod]
    [DataRow("-\ta: b", DisplayName = "a compact mapping")]
    [DataRow("-\t- a", DisplayName = "a compact sequence")]
    [DataRow("- \t\"a\": b", DisplayName = "a compact mapping with a quoted key")]
    [DataRow("-\t? a", DisplayName = "a compact mapping with an explicit key")]
    public void Parse_WhenTabIndentsCompactCollectionAfterDash_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a tab between a sequence entry's dash and a scalar on its line is separation and is accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTabSeparatesDashFromScalar_ShouldAccept()
    {
        using var document = YamlDocument.Parse("-\ta\n-\t\"b c\"\n");

        Assert.AreEqual("""["a","b c"]""", ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that invalid UTF-8 input is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidUtf8_ShouldThrow()
    {
        byte[] bytes = new byte[] { (byte)'a', (byte)':', (byte)' ', 0xFF };

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse(bytes);
        });
    }

    /// <summary>
    /// Verifies that a raw non-printable control character is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenControlCharacter_ShouldThrow()
    {
        byte[] bytes = new byte[] { (byte)'a', (byte)':', (byte)' ', 0x01 };

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse(bytes);
        });
    }

    /// <summary>
    /// Verifies that a double-quoted escape resolving to a surrogate code point is rejected with
    /// <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSurrogateEscape_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: \"\\uD800\"\n");
        });
    }

    /// <summary>
    /// Verifies that a double-quoted escape above the Unicode range is rejected with
    /// <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOutOfRangeEscape_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: \"\\U00110000\"\n");
        });
    }

    /// <summary>Verifies that nesting beyond the configured maximum depth is rejected.</summary>
    [TestMethod]
    public void Parse_WhenNestingExceedsMaxDepth_ShouldThrow()
    {
        string deep = string.Concat(Enumerable.Repeat("[", 200)) + "x" + string.Concat(Enumerable.Repeat("]", 200));
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("root: " + deep + "\n");
        });
    }

    /// <summary>
    /// Verifies that a flow collection in implicit-key position is rejected wherever the key appears, rather than its
    /// brackets being read as the text of a scalar key.
    /// </summary>
    /// <param name="yaml">A document with a flow collection as an implicit key.</param>
    [TestMethod]
    [DataRow("1: 2\n\n[3]: 4", DisplayName = "after a blank line")]
    [DataRow("1: 2\n# c\n[3]: 4", DisplayName = "after a comment line")]
    [DataRow("?\n[3]: 4", DisplayName = "after an empty explicit key")]
    [DataRow("k: v\n[a, b]: c\n", DisplayName = "a flow sequence, later key")]
    [DataRow("k: v\n{a: b}: c\n", DisplayName = "a flow mapping, later key")]
    [DataRow("- k: v\n  [a]: b", DisplayName = "a later key of a nested mapping")]
    [DataRow("&a [3]: 4", DisplayName = "an anchored first key")]
    [DataRow("k: v\n&a [3]: 4", DisplayName = "an anchored later key")]
    [DataRow("k: v\n!!seq [3]: 4", DisplayName = "a tagged later key")]
    public void Parse_WhenFlowCollectionIsImplicitKey_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }
}
