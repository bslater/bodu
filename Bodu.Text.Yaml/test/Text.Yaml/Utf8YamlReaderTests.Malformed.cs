// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlReaderTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlReader" /> rejects malformed documents with <see cref="YamlFormatException" /> —
/// the malformed-input sweep every sibling reader carries (<c>Utf8TomlReaderTests.Malformed</c>,
/// <c>Utf8BencodeReaderTests.Malformed</c>). Each row names the grammar or profile rule it violates.
/// </summary>
public partial class Utf8YamlReaderTests
{
    /// <summary>
    /// Verifies that a malformed document is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    /// <param name="testName">The scenario label naming the violated rule.</param>
    /// <param name="yaml">The malformed YAML text.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("unterminated double-quoted scalar", "text: \"abc\n")]
    [DataRow("unterminated single-quoted scalar", "text: 'abc\n")]
    [DataRow("invalid escape sequence", "text: \"a\\qb\"\n")]
    [DataRow("truncated unicode escape", "text: \"\\u00\"\n")]
    [DataRow("surrogate unicode escape", "text: \"\\uD800\"\n")]
    [DataRow("out-of-range unicode escape", "text: \"\\U00110000\"\n")]
    [DataRow("under-indented quoted continuation", "text: \"line1\nline2\"\n")]
    [DataRow("escaped continuation onto document marker", "text: \"line1 \\\n--- x\"\n")]
    [DataRow("mapping value indicator inside plain scalar", "a: b : c\n")]
    [DataRow("tab indentation for block mapping", "a:\n\tb: 1\n")]
    [DataRow("alias to undefined anchor", "a: *missing\n")]
    [DataRow("anchor redefinition (profile restriction)", "a: &x 1\nb: &x 2\n")]
    [DataRow("cyclic alias", "a: &a\n  b: *a\n")]
    [DataRow("non-core tag", "a: !custom v\n")]
    [DataRow("collection tag on scalar", "a: !!map v\n")]
    [DataRow("scalar tag on sequence", "a: !!int [1, 2]\n")]
    [DataRow("complex mapping key (profile restriction)", "? [1, 2]\n: v\n")]
    [DataRow("unterminated flow sequence", "a: [1, 2\n")]
    [DataRow("unterminated flow mapping", "a: {b: 1\n")]
    [DataRow("reserved indicator at scalar start", "a: `x\n")]
    [DataRow("at-sign indicator at scalar start", "a: @x\n")]
    [DataRow("duplicate mapping key", "a: 1\na: 2\n")]
    public void Read_WhenDocumentMalformed_ShouldThrowYamlFormatException(string testName, string yaml)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(yaml);

        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            var reader = new Utf8YamlReader(bytes);
            while (reader.Read())
            {
                // Drain the document; construction or any Read may surface the rejection.
            }
        });

        Assert.IsNotNull(ex.Message, testName);
    }

    /// <summary>
    /// Verifies that a document carrying invalid UTF-8 is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenInvalidUtf8_ShouldThrowYamlFormatException()
    {
        byte[] bytes = [(byte)'a', (byte)':', (byte)' ', 0xC3, 0x28, (byte)'\n'];

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            var reader = new Utf8YamlReader(bytes);
            while (reader.Read())
            {
                // Drain.
            }
        });
    }

    /// <summary>
    /// Verifies that nesting deeper than the configured maximum depth is rejected with
    /// <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenNestingExceedsMaxDepth_ShouldThrowYamlFormatException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("a: [[[1]]]\n");

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            var reader = new Utf8YamlReader(bytes, new YamlReaderOptions { MaxDepth = 2 });
            while (reader.Read())
            {
                // Drain.
            }
        });
    }
}
