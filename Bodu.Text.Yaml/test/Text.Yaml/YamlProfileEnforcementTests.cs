// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlProfileEnforcementTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that the parser enforces the library's YAML 1.2 core, JSON-compatible tree profile by rejecting the
/// constructs that the profile excludes: duplicate keys, complex keys, duplicate anchors, tabs in indentation, invalid
/// UTF-8, non-printable control characters, and invalid Unicode escapes.
/// </summary>
[TestClass]
public sealed class YamlProfileEnforcementTests
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
    /// Verifies that invalid UTF-8 input is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInvalidUtf8_ShouldThrow()
    {
        var bytes = new byte[] { (byte)'a', (byte)':', (byte)' ', 0xFF };

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
        var bytes = new byte[] { (byte)'a', (byte)':', (byte)' ', 0x01 };

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
}
