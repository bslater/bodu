// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.Scalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies scalar emission rules of <see cref="Utf8YamlWriter" />: ambiguous-string quoting and special-character
/// escaping.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>Verifies that a string which would resolve to another type is quoted.</summary>
    [TestMethod]
    public void Write_WhenAmbiguousString_ShouldQuote()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("a");
            w.WriteString("123");
            w.WritePropertyName("b");
            w.WriteString("true");
            w.WritePropertyName("c");
            w.WriteString("no");
            w.WriteEndMapping();
        });

        Assert.AreEqual("a: \"123\"\nb: \"true\"\nc: \"no\"\n", yaml);
    }

    /// <summary>Verifies that a value containing special characters is double-quoted with escapes.</summary>
    [TestMethod]
    public void Write_WhenSpecialCharacters_ShouldEscape()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("text");
            w.WriteString("line1\nline2\ttab");
            w.WriteEndMapping();
        });

        Assert.AreEqual("text: \"line1\\nline2\\ttab\"\n", yaml);
    }
}
